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

        public IList<Products> Products { get; set; }

        // Sorting
        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; }

        // Search
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // ---------------------------------------------------------
        // ADD TO LIST HANDLER
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostAddToListAsync(int productId)
        {
            // Must be signed in
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            // Bridge Identity user → custom User table via email
            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            int userId = appUser.UserID;

            // Check if user already has a list
            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.userID == userId);

            // If no list → create one
            if (list == null)
            {
                list = new Product_list
                {
                    userID = userId,
                    total_price = 0,
                    list_type = ListType.User,
                    created_at = DateTime.UtcNow
                };

                _listContext.Product_list.Add(list);
                await _listContext.SaveChangesAsync();
            }

            // Get product
            var product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == productId);

            if (product == null)
                return RedirectToPage("/Error");

            // Add item to list
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

            // Update total price
            list.total_price += product.retail_price;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.product_name} added to your list!";
            return RedirectToPage();
        }

        // ---------------------------------------------------------
        // GET: Catalog Page
        // ---------------------------------------------------------
        public async Task OnGetAsync()
        {
            var apiProducts = new List<Products>();

            bool searchMatch = catalogListModel.AllowedSearches.Any(s =>
                Search != null && (
                    Search.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    s.Contains(Search, StringComparison.OrdinalIgnoreCase)
                ));

            // ---------------------------------------------------------
            // SERP API INGESTION
            // ---------------------------------------------------------
            if (!string.IsNullOrEmpty(Search) && searchMatch)
            {
                var results = _serpApiService.SearchProducts(Search);

                int resultLimit = 0;

                foreach (JObject result in results ?? new JArray())
                {
                    if (resultLimit >= 20)
                        break;

                    var product = new Products
                    {
                        product_name = result["title"]?.ToString()?[..Math.Min(result["title"]?.ToString()?.Length ?? 0, 50)] ?? "",
                        description = result["description"]?.ToString() ?? "",
                        retail_URL = result["serpapi_link"]?.ToString() ?? "",
                        ImageURL = result["thumbnail"]?.ToString() ?? "",
                        source_name = result["source"]?.ToString(),
                        source_logo = result["source_icon"]?.ToString(),
                        rating = result["rating"]?.ToObject<double?>(),
                        reviews = result["reviews"]?.ToObject<int?>(),
                    };

                    if (double.TryParse(result["extracted_price"]?.ToString(), out double retailPrice))
                        product.retail_price = retailPrice;
                    else
                        product.retail_price = 0;

                    apiProducts.Add(product);
                    resultLimit++;
                }

                _productsContext.Products.AddRange(apiProducts);
                await _productsContext.SaveChangesAsync();

                // Remove duplicate products
                var allProducts = await _productsContext.Products.ToListAsync();
                var duplicates = allProducts
                    .GroupBy(p => p.product_name)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderBy(p => p.product_ID).Skip(1))
                    .ToList();

                if (duplicates.Any())
                {
                    _productsContext.Products.RemoveRange(duplicates);
                    await _productsContext.SaveChangesAsync();
                }
            }

            // ---------------------------------------------------------
            // QUERY + FILTERS
            // ---------------------------------------------------------
            var query = _productsContext.Products.AsQueryable();

            if (!string.IsNullOrEmpty(Search))
            {
                query = query.Where(p =>
                    p.product_name.Contains(Search) ||
                    p.description.Contains(Search));
            }

            // Regular users cannot see bulk items
            if (!User.IsInRole("Admin") && !User.IsInRole("School"))
            {
                query = query.Where(p => p.bulk_availability == false);
            }

            // Sorting
            query = Sort switch
            {
                "name_asc" => query.OrderBy(p => p.product_name),
                "name_desc" => query.OrderByDescending(p => p.product_name),
                "price_asc" => query.OrderBy(p => p.retail_price),
                "price_desc" => query.OrderByDescending(p => p.retail_price),
                _ => query.OrderBy(p => p.product_name)
            };

            // Pagination
            if (PageNumber < 1)
                PageNumber = 1;

            TotalItems = await query.CountAsync();

            if (TotalItems > 0 && PageNumber > TotalPages)
                PageNumber = TotalPages;

            Products = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
