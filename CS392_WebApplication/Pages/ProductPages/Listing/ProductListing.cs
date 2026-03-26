using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.ProductPages.Listing
{
    public class ProductListingModel : PageModel
    {
        private readonly ProductsDbContext _productsContext;
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ProductListingModel> _logger;

        public ProductListingModel(
            ILogger<ProductListingModel> logger,
            ProductsDbContext productsContext,
            Product_listDbContext listContext,
            UserDbContext userContext,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _productsContext = productsContext;
            _listContext = listContext;
            _userContext = userContext;
            _userManager = userManager;
        }

        public Products Product { get; set; } = default!;

        // GET
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == id);

            if (Product == null)
                return NotFound();

            return Page();
        }

        // ADD TO LIST
        public async Task<IActionResult> OnPostAddToListAsync(int productId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.userID == appUser.UserID);

            if (list == null)
            {
                list = new Product_list
                {
                    userID = appUser.UserID,
                    total_price = 0,
                    list_type = ListType.User,
                    created_at = DateTime.UtcNow
                };

                _listContext.Product_list.Add(list);
                await _listContext.SaveChangesAsync();
            }

            var product = await _productsContext.Products
                .FirstOrDefaultAsync(p => p.product_ID == productId);

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
            TempData["UndoProductId"] = productId;

            return RedirectToPage(new { id = productId });
        }

        // UNDO ADD
        public async Task<IActionResult> OnPostUndoAddAsync(int productId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.userID == appUser.UserID);

            var item = await _productsContext.Product_list_items
                .Where(i => i.list_ID == list.listID && i.product_ID == productId)
                .OrderByDescending(i => i.list_items_ID)
                .FirstOrDefaultAsync();

            if (item != null)
            {
                list.total_price -= item.price_at_purchase;
                _productsContext.Product_list_items.Remove(item);
                await _productsContext.SaveChangesAsync();
                await _listContext.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Item removed from your list.";

            return RedirectToPage(new { id = productId });
        }
    }
}
