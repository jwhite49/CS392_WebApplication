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

        public List<Product_list> UserLists { get; set; } = new();
        public Dictionary<int, int> ListItemCounts { get; set; } = new();
        public HashSet<int> ImportedListIds { get; set; } = new();
        public bool IsAdminOrSchool { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            IsAdminOrSchool = User.IsInRole("Admin") || User.IsInRole("School");

            UserLists = await _listContext.Product_list
                .Where(l => l.userID == appUser.UserID)
                .OrderByDescending(l => l.updated_at)
                .ToListAsync();

            // Determine which lists are imported from published lists
            var imported = await _listContext.PublishedList_Student
                .Where(ps => ps.student_userID == appUser.UserID)
                .Select(ps => ps.student_listID)
                .ToListAsync();

            ImportedListIds = new HashSet<int>(imported);

            foreach (var list in UserLists)
            {
                var count = await _productsContext.Product_list_items
                    .CountAsync(i => i.list_ID == list.listID);
                ListItemCounts[list.listID] = count;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteListAsync(int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);

            if (list == null)
            {
                TempData["ErrorMessage"] = "List not found.";
                return RedirectToPage();
            }

            // Remove all items in this list
            var items = await _productsContext.Product_list_items
                .Where(i => i.list_ID == listId).ToListAsync();
            _productsContext.Product_list_items.RemoveRange(items);
            await _productsContext.SaveChangesAsync();

            // Remove published-student records referencing this list
            var studentRecords = await _listContext.PublishedList_Student
                .Where(ps => ps.published_listID == listId || ps.student_listID == listId)
                .ToListAsync();
            _listContext.PublishedList_Student.RemoveRange(studentRecords);

            _listContext.Product_list.Remove(list);
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "List deleted successfully.";
            return RedirectToPage();
        }
    }
}
