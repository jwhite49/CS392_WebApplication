using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;

namespace CS392_WebApplication.Pages.Lists
{
    public class ListModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly ProductsDbContext _productsContext;
        private readonly UserManager<IdentityUser> _userManager;

        public ListModel(
            Product_listDbContext listContext,
            UserDbContext userContext,
            ProductsDbContext productsContext,
            UserManager<IdentityUser> userManager)
        {
            _listContext = listContext;
            _userContext = userContext;
            _productsContext = productsContext;
            _userManager = userManager;
        }

        public Product_list? UserList { get; set; }
        public bool HasList => UserList != null;

        public List<(Product_list_items Item, Products Product)> ListItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login");

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            UserList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.userID == appUser.UserID);

            if (UserList == null)
                return Page();

            var items = await _productsContext.Product_list_items
                .Where(i => i.list_ID == UserList.listID)
                .ToListAsync();

            foreach (var item in items)
            {
                var product = await _productsContext.Products
                    .FirstOrDefaultAsync(p => p.product_ID == item.product_ID);

                if (product != null)
                    ListItems.Add((item, product));
            }

            return Page();
        }

        // DELETE ITEM
        public async Task<IActionResult> OnPostDeleteItemAsync(int listItemId)
        {
            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == listItemId);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage();
            }

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == item.list_ID);

            if (list != null)
            {
                list.total_price -= item.price_at_purchase * item.quantity;
                if (list.total_price < 0)
                    list.total_price = 0;

                await _listContext.SaveChangesAsync();
            }

            _productsContext.Product_list_items.Remove(item);
            await _productsContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item removed from your list.";
            return RedirectToPage();
        }
    }
}
