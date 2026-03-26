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

        // Category filter
        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

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
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            int userId = appUser.UserID;

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.userID == userId);

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
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.product_name} added to your list!";
            return RedirectToPage();
        }

        // ---------------------------------------------------------
        // GET: Catalog Page
        // ---------------------------------------------------------
        public async Task OnGetAsync()
        {
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
        }
    }
}
