using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using CS392_WebApplication.API;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace CS392_WebApplication.Pages.ProductPages.Listing
{
    public class ProductListingModel : PageModel
    {
        private readonly ProductsDbContext _productsContext;
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ProductListingModel> _logger;
        private readonly apiConfig _serpApiService;

        public ProductListingModel(
            ILogger<ProductListingModel> logger,
            ProductsDbContext productsContext,
            Product_listDbContext listContext,
            UserDbContext userContext,
            UserManager<IdentityUser> userManager,
            apiConfig serpApiService)
        {
            _logger = logger;
            _productsContext = productsContext;
            _listContext = listContext;
            _userContext = userContext;
            _userManager = userManager;
            _serpApiService = serpApiService;
        }

        public Products Product { get; set; } = default!;

        // Price comparisons from SerpAPI
        public List<(string Source, string? Logo, double Price, string Url)> PriceComparisons { get; set; } = new();

        // User's lists for the picker
        public List<Product_list> UserLists { get; set; } = new();
        public HashSet<int> ImportedListIds { get; set; } = new();

        // Track which list was just added to (for undo)
        public int? LastAddedListId { get; set; }

        // GET
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == id);

            if (Product == null)
                return NotFound();

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

            // Restore last added list ID from TempData for undo
            if (TempData.Peek("UndoListId") is int undoListId)
                LastAddedListId = undoListId;

            // PRICE COMPARISONS: search SerpAPI for other retailers selling this product
            try
            {
                var results = _serpApiService.SearchProducts(Product.product_name);
                foreach (JObject result in results ?? new JArray())
                {
                    if (PriceComparisons.Count >= 6) break;

                    var url = result["serpapi_link"]?.ToString()?.Trim() is { Length: > 0 } u ? u
                              : result["link"]?.ToString();
                    var source = result["source"]?.ToString();

                    if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(source)) continue;

                    // Skip if it's the same source already shown
                    if (source.Equals(Product.source_name, StringComparison.OrdinalIgnoreCase)) continue;

                    double.TryParse(result["extracted_price"]?.ToString(), out double price);
                    var logo = result["source_icon"]?.ToString();
                    PriceComparisons.Add((source, logo, price, url));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SerpAPI price comparison failed for product {Id}", id);
            }

            return Page();
        }

        // ADD TO LIST (now accepts listId)
        public async Task<IActionResult> OnPostAddToListAsync(int productId, int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            // Verify list belongs to user
            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser!.UserID);

            if (list == null)
            {
                TempData["ErrorMessage"] = "List not found.";
                return RedirectToPage(new { id = productId });
            }

            // Block adding to imported lists
            var isImported = await _listContext.PublishedList_Student
                .AnyAsync(ps => ps.student_listID == listId && ps.student_userID == appUser!.UserID);

            if (isImported)
            {
                TempData["ErrorMessage"] = "Cannot add items to an imported list.";
                return RedirectToPage(new { id = productId });
            }

            var product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == productId);

            var item = new Product_list_items
            {
                list_ID = list.listID,
                product_ID = productId,
                quantity = 1,
                price_at_purchase = (float)product!.retail_price,
                purchase_type = Product_list_items.PurchaseType.Retail
            };

            _productsContext.Product_list_items.Add(item);
            await _productsContext.SaveChangesAsync();

            list.total_price += product.retail_price;
            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.product_name} added to \"{list.title}\"!";
            TempData["UndoProductId"] = productId;
            TempData["UndoListId"] = listId;

            return RedirectToPage(new { id = productId });
        }

        // UNDO ADD (now list-aware)
        public async Task<IActionResult> OnPostUndoAddAsync(int productId, int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser!.UserID);

            if (list != null)
            {
                var item = await _productsContext.Product_list_items
                    .Where(i => i.list_ID == list.listID && i.product_ID == productId)
                    .OrderByDescending(i => i.list_items_ID)
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    list.total_price -= item.price_at_purchase;
                    if (list.total_price < 0) list.total_price = 0;
                    _productsContext.Product_list_items.Remove(item);
                    await _productsContext.SaveChangesAsync();
                    await _listContext.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Item removed from your list.";

            return RedirectToPage(new { id = productId });
        }
    }
}
