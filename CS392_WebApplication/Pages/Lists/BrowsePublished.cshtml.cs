using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;

namespace CS392_WebApplication.Pages.Lists
{
    public class BrowsePublishedModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly ProductsDbContext _productsContext;
        private readonly UserManager<IdentityUser> _userManager;

        public BrowsePublishedModel(
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

        public List<PublishedListViewModel> PublishedLists { get; set; } = new();
        public HashSet<int> AlreadyAddedListIds { get; set; } = new();

        public class PublishedListViewModel
        {
            public Product_list List { get; set; } = default!;
            public string OwnerName { get; set; } = "Unknown";
            public int ItemCount { get; set; }
            public int StudentCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            // Get all published lists
            var publishedLists = await _listContext.Product_list
                .Where(l => l.is_published)
                .OrderByDescending(l => l.updated_at)
                .ToListAsync();

            // Find which ones the current user already added
            var userImports = await _listContext.PublishedList_Student
                .Where(ps => ps.student_userID == appUser.UserID)
                .Select(ps => ps.published_listID)
                .ToListAsync();

            AlreadyAddedListIds = new HashSet<int>(userImports);

            foreach (var list in publishedLists)
            {
                var owner = await _userContext.User
                    .FirstOrDefaultAsync(u => u.UserID == list.userID);

                var itemCount = await _productsContext.Product_list_items
                    .CountAsync(i => i.list_ID == list.listID);

                var studentCount = await _listContext.PublishedList_Student
                    .CountAsync(ps => ps.published_listID == list.listID);

                PublishedLists.Add(new PublishedListViewModel
                {
                    List = list,
                    OwnerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown",
                    ItemCount = itemCount,
                    StudentCount = studentCount
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToMyListsAsync(int publishedListId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            // Check if already added
            var alreadyAdded = await _listContext.PublishedList_Student
                .AnyAsync(ps => ps.published_listID == publishedListId
                             && ps.student_userID == appUser.UserID);

            if (alreadyAdded)
            {
                TempData["ErrorMessage"] = "You have already added this list.";
                return RedirectToPage();
            }

            // Get the original published list
            var originalList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == publishedListId && l.is_published);

            if (originalList == null)
            {
                TempData["ErrorMessage"] = "Published list not found.";
                return RedirectToPage();
            }

            // Create a copy for the student
            var studentList = new Product_list
            {
                userID = appUser.UserID,
                title = originalList.title,
                description = originalList.description,
                grade_level = originalList.grade_level,
                total_price = originalList.total_price,
                list_type = ListType.User,
                is_published = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _listContext.Product_list.Add(studentList);
            await _listContext.SaveChangesAsync();

            // Copy all items
            var originalItems = await _productsContext.Product_list_items
                .Where(i => i.list_ID == publishedListId)
                .ToListAsync();

            foreach (var item in originalItems)
            {
                var copy = new Product_list_items
                {
                    list_ID = studentList.listID,
                    product_ID = item.product_ID,
                    quantity = item.recommended_quantity ?? item.quantity,
                    price_at_purchase = item.price_at_purchase,
                    purchase_type = item.purchase_type,
                    is_required = item.is_required,
                    recommended_quantity = item.recommended_quantity,
                    teacher_note = item.teacher_note
                };
                _productsContext.Product_list_items.Add(copy);
            }
            await _productsContext.SaveChangesAsync();

            // Create the published-student link
            var link = new PublishedList_Student
            {
                published_listID = publishedListId,
                student_listID = studentList.listID,
                student_userID = appUser.UserID,
                added_at = DateTime.UtcNow,
                is_completed = false
            };

            _listContext.PublishedList_Student.Add(link);
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "List added to your lists!";
            return RedirectToPage();
        }
    }
}