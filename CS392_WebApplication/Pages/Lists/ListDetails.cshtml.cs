using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;

namespace CS392_WebApplication.Pages.Lists
{
    public class ListDetailsModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly ProductsDbContext _productsContext;
        private readonly UserManager<IdentityUser> _userManager;

        public ListDetailsModel(
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

        public Product_list UserList { get; set; } = default!;
        public List<(Product_list_items Item, Products Product)> ListItems { get; set; } = new();

        // If this list was imported from a published list, track that
        public PublishedList_Student? ImportRecord { get; set; }

        // Imported lists are read-only (no add/remove/quantity changes)
        public bool IsReadOnly => ImportRecord != null;

        public async Task<IActionResult> OnGetAsync(int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            UserList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);

            if (UserList == null)
            {
                TempData["ErrorMessage"] = "List not found.";
                return RedirectToPage("/Lists/List");
            }

            // Check if this list was imported from a published list
            ImportRecord = await _listContext.PublishedList_Student
                .FirstOrDefaultAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

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

        public async Task<IActionResult> OnPostDeleteItemAsync(int listItemId, int listId)
        {
            // Block if imported
            if (await IsImportedList(listId))
            {
                TempData["ErrorMessage"] = "Cannot modify an imported list.";
                return RedirectToPage(new { listId });
            }

            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == listItemId);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage(new { listId });
            }

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == item.list_ID);

            if (list != null)
            {
                list.total_price -= item.price_at_purchase * item.quantity;
                if (list.total_price < 0) list.total_price = 0;
                list.updated_at = DateTime.UtcNow;
                await _listContext.SaveChangesAsync();
            }

            _productsContext.Product_list_items.Remove(item);
            await _productsContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item removed from your list.";
            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostIncreaseQuantityAsync(int listItemId, int listId)
        {
            if (await IsImportedList(listId))
            {
                TempData["ErrorMessage"] = "Cannot modify an imported list.";
                return RedirectToPage(new { listId });
            }

            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == listItemId);

            if (item != null)
            {
                item.quantity++;
                await _productsContext.SaveChangesAsync();

                // Update list total
                var list = await _listContext.Product_list
                    .FirstOrDefaultAsync(l => l.listID == item.list_ID);
                if (list != null)
                {
                    list.total_price += item.price_at_purchase;
                    list.updated_at = DateTime.UtcNow;
                    await _listContext.SaveChangesAsync();
                }
            }

            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostDecreaseQuantityAsync(int listItemId, int listId)
        {
            if (await IsImportedList(listId))
            {
                TempData["ErrorMessage"] = "Cannot modify an imported list.";
                return RedirectToPage(new { listId });
            }

            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == listItemId);

            if (item != null && item.quantity > 1)
            {
                item.quantity--;
                await _productsContext.SaveChangesAsync();

                var list = await _listContext.Product_list
                    .FirstOrDefaultAsync(l => l.listID == item.list_ID);
                if (list != null)
                {
                    list.total_price -= item.price_at_purchase;
                    if (list.total_price < 0) list.total_price = 0;
                    list.updated_at = DateTime.UtcNow;
                    await _listContext.SaveChangesAsync();
                }
            }

            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostToggleCompleteAsync(int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            var record = await _listContext.PublishedList_Student
                .FirstOrDefaultAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

            if (record != null)
            {
                record.is_completed = !record.is_completed;
                await _listContext.SaveChangesAsync();

                TempData["SuccessMessage"] = record.is_completed
                    ? "List marked as completed!"
                    : "List marked as incomplete.";
            }

            return RedirectToPage(new { listId });
        }

        private async Task<bool> IsImportedList(int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return false;

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return false;

            return await _listContext.PublishedList_Student
                .AnyAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);
        }
    }
}