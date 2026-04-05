using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using CS392_WebApplication.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace CS392_WebApplication.Pages.ProductPages.Catalog
{
    public class IndexModel : PageModel
    {
        private readonly ProductsDbContext _productsContext;
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly apiConfig _serpApiService;

        public IndexModel(
            ProductsDbContext productsContext,
            Product_listDbContext listContext,
            UserDbContext userContext,
            UserManager<IdentityUser> userManager,
            apiConfig serpApiService)
        {
            _productsContext = productsContext;
            _listContext = listContext;
            _userContext = userContext;
            _userManager = userManager;
            _serpApiService = serpApiService;
        }

        public IList<Products> Products { get; set; } = new List<Products>();

        // User's lists for the "Add to List" picker
        public List<Product_list> UserLists { get; set; } = new();

        // Imported (read-only) list IDs so we can exclude them from the picker
        public HashSet<int> ImportedListIds { get; set; } = new();

        // Sorting
        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        // Search
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        // Category filter
        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // ---------------------------------------------------------
        // ADD TO LIST HANDLER (now accepts listId)
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostAddToListAsync(int productId, int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            // Verify the list belongs to the user
            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);

            if (list == null)
            {
                TempData["ErrorMessage"] = "List not found.";
                return RedirectToPage();
            }

            // Block adding to imported (read-only) lists
            var isImported = await _listContext.PublishedList_Student
                .AnyAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

            if (isImported)
            {
                TempData["ErrorMessage"] = "Cannot add items to an imported list.";
                return RedirectToPage();
            }

            var product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == productId);

            if (product == null)
                return RedirectToPage("/Error");

            var item = new Product_list_items
            {
                list_ID = list.listID,
                product_ID = productId,
                quantity = 1,
                price_at_purchase = (float)product.retail_price,
                purchase_type = Product_list_items.PurchaseType.Retail
            };

            _productsContext.Product_list_items.Add(item);
            await _productsContext.SaveChangesAsync();

            list.total_price += product.retail_price;
            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.product_name} added to \"{list.title}\"!";
            return RedirectToPage();
        }

        // ---------------------------------------------------------
        // GET: Catalog Page
        // ---------------------------------------------------------
        public async Task OnGetAsync()
        {
            // Load user's lists for the picker
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser != null)
            {
                var appUser = await _userContext.User
                    .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

                if (appUser != null)
                {
                    UserLists = await _listContext.Product_list
                        .Where(l => l.userID == appUser.UserID)
                        .OrderByDescending(l => l.updated_at)
                        .ToListAsync();

                    var imported = await _listContext.PublishedList_Student
                        .Where(ps => ps.student_userID == appUser.UserID)
                        .Select(ps => ps.student_listID)
                        .ToListAsync();

                    ImportedListIds = new HashSet<int>(imported);
                }
            }

            var query = _productsContext.Products.AsQueryable();

            // CATEGORY FILTER
            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(p =>
                    p.product_name.Contains(Category) ||
                    p.description.Contains(Category));
            }

            // SEARCH FILTER
            if (!string.IsNullOrEmpty(Search))
            {
                query = query.Where(p =>
                    p.product_name.Contains(Search) ||
                    p.description.Contains(Search));
            }

            // REGULAR USERS CANNOT SEE BULK ITEMS
            if (!User.IsInRole("Admin") && !User.IsInRole("School"))
            {
                query = query.Where(p => p.bulk_availability == false);
            }

            // SORTING
            query = Sort switch
            {
                "name_asc" => query.OrderBy(p => p.product_name),
                "name_desc" => query.OrderByDescending(p => p.product_name),
                "price_asc" => query.OrderBy(p => p.retail_price),
                "price_desc" => query.OrderByDescending(p => p.retail_price),
                _ => query.OrderBy(p => p.product_name)
            };

            // PAGINATION
            if (PageNumber < 1)
                PageNumber = 1;

            TotalItems = await query.CountAsync();

            if (TotalItems > 0 && PageNumber > TotalPages)
                PageNumber = TotalPages;

            Products = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            //API INGESTION - Only call API if no products found in DB, search field not empty, and if search matches a term on whitelist
            bool isAllowedSearch = !string.IsNullOrEmpty(Search) && catalogListModel.AllowedSearches.Any(a =>
                a.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                Search.Contains(a, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(Search) && !Products.Any() && isAllowedSearch)
            {
                var apiProducts = new List<Products>();
                var results = _serpApiService.SearchProducts(Search);
                int resultLimit = 0;

                foreach (JObject result in results ?? new JArray())
                {
                    if (resultLimit >= 30) break;

                    var product = new Products
                    {
                        product_name = result["title"]?.ToString()?[..Math.Min(result["title"]?.ToString()?.Length ?? 0, 50)] ?? "Unknown Product",
                        description = result["description"]?.ToString()?.Trim() is { Length: > 0 } d ? d : "No description available.",
                        retail_URL = result["serpapi_link"]?.ToString()?.Trim() is { Length: > 0 } u ? u : result["link"]?.ToString() ?? "#",
                        ImageURL = result["thumbnail"]?.ToString(),
                        source_name = result["source"]?.ToString() is { Length: > 0 } sn ? sn[..Math.Min(sn.Length, 40)] : null,
                        source_logo = result["source_icon"]?.ToString() is { Length: > 0 } sl ? sl[..Math.Min(sl.Length, 255)] : null,
                        rating = result["rating"]?.ToObject<double?>(),
                        reviews = result["reviews"]?.ToObject<int?>(),
                        bulk_availability = false,
                    };

                    if (double.TryParse(result["extracted_price"]?.ToString(), out double retailPrice))
                        product.retail_price = retailPrice;
                    else
                        product.retail_price = 0;

                    apiProducts.Add(product);
                    resultLimit++;
                }

                if (apiProducts.Any())
                {
                    _productsContext.Products.AddRange(apiProducts);
                    await _productsContext.SaveChangesAsync();

                    TotalItems = apiProducts.Count;
                    Products = apiProducts
                        .Skip((PageNumber - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }
            }
        }
    }
}
