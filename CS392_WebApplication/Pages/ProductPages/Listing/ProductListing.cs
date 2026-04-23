using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using CS392_WebApplication.API;
using CS392_WebApplication.Services;
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
        private readonly GeminiService _geminiService;
        private readonly SystemLogDbContext _systemLogContext;

        public ProductListingModel(
            ILogger<ProductListingModel> logger,
            ProductsDbContext productsContext,
            Product_listDbContext listContext,
            UserDbContext userContext,
            UserManager<IdentityUser> userManager,
            apiConfig serpApiService,
            GeminiService geminiService,
            SystemLogDbContext systemLogContext)
        {
            _logger = logger;
            _productsContext = productsContext;
            _listContext = listContext;
            _userContext = userContext;
            _userManager = userManager;
            _serpApiService = serpApiService;
            _geminiService = geminiService;
            _systemLogContext = systemLogContext;
        }

        public Products Product { get; set; } = default!;

        // Price comparisons from SerpAPI
        public List<(string Source, string? Logo, double Price, string Url)> PriceComparisons { get; set; } = new();

        // User's lists for the picker
        public List<Product_list> UserLists { get; set; } = new();
        public HashSet<int> ImportedListIds { get; set; } = new();

        // Track which list was just added to (for undo)
        public int? LastAddedListId { get; set; }

        // Recommended similar products
        public List<Products> RecommendedProducts { get; set; } = new();

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

            // PRICE COMPARISONS: use SerpAPI product offers endpoint if we have a product_id,
            // otherwise fall back to a name search (gives serpapi_link results which aren't direct buy links).
            try
            {
                JArray offers;

                if (!string.IsNullOrEmpty(Product.google_product_id))
                {
                    // Best path: real retailer offer URLs from the product details endpoint
                    offers = await _serpApiService.GetProductOffersAsync(Product.google_product_id);
                    foreach (JObject offer in offers)
                    {
                        if (PriceComparisons.Count >= 6) break;

                        var url = offer["link"]?.ToString();
                        var source = offer["name"]?.ToString();

                        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(source)) continue;
                        if (source.Equals(Product.source_name, StringComparison.OrdinalIgnoreCase)) continue;

                        double.TryParse(offer["extracted_price"]?.ToString(), out double price);
                        var logo = offer["source_icon"]?.ToString();
                        PriceComparisons.Add((source, logo, price, url));
                    }
                }
                else
                {
                    // Fallback: search by name — links will be serpapi_link (not direct retailer URLs)
                    var results = _serpApiService.SearchProducts(Product.product_name);
                    foreach (JObject result in results ?? new JArray())
                    {
                        if (PriceComparisons.Count >= 6) break;

                        var url = result["link"]?.ToString();
                        var source = result["source"]?.ToString();

                        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(source)) continue;
                        if (source.Equals(Product.source_name, StringComparison.OrdinalIgnoreCase)) continue;

                        double.TryParse(result["extracted_price"]?.ToString(), out double price);
                        var logo = result["source_icon"]?.ToString();
                        PriceComparisons.Add((source, logo, price, url));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SerpAPI price comparison failed for product {Id}", id);
            }

            // RECOMMENDED: find up to 5 products that share keywords with this product's name
            var keywords = Product.product_name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Select(w => w.ToLower())
                .ToList();

            if (keywords.Any())
            {
                var allOthers = await _productsContext.Products
                    .Where(p => p.product_ID != Product.product_ID)
                    .ToListAsync();

                RecommendedProducts = allOthers
                    .Select(p => new
                    {
                        Product = p,
                        Score = keywords.Count(kw =>
                            p.product_name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(5)
                    .Select(x => x.Product)
                    .ToList();
            }

            return Page();
        }

        // GEMINI AI PRICE SEARCH (AJAX endpoint)
        public async Task<IActionResult> OnGetGeminiPricesAsync(int id)
        {
            var product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == id);

            if (product == null)
                return new JsonResult(new { success = true, data = "[]" });

            try
            {
                var result = await _geminiService.SearchLivePricesAsync(
                    product.product_name, product.retail_price, product.source_name);
                return new JsonResult(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini price search failed for product {Id}", id);

                try
                {
                    _systemLogContext.SystemLog.Add(new CS392_WebApplication.Models.SystemLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "Error",
                        EventType = "GeminiPriceSearch",
                        Message = $"Gemini AI price search failed for product ID {id}: {ex.Message}",
                        StackTrace = ex.StackTrace,
                        Page = "/ProductPages/Listing"
                    });
                    await _systemLogContext.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to write Gemini error to SystemLog");
                }

                // Return empty result — do not expose error details to the user
                return new JsonResult(new { success = true, data = "[]" });
            }
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
