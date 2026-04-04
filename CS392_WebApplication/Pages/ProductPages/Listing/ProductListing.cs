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
