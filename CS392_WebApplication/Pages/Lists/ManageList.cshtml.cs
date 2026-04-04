using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;

namespace CS392_WebApplication.Pages.Lists
{
    [Authorize(Roles = "Admin,School")]
    public class ManageListModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly ProductsDbContext _productsContext;
        private readonly UserManager<IdentityUser> _userManager;

        public ManageListModel(
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

        public Product_list ManagedList { get; set; } = default!;
        public List<StudentViewModel> Students { get; set; } = new();
        public List<(Product_list_items Item, Products Product)> ListItems { get; set; } = new();

        public class StudentViewModel
        {
            public string Name { get; set; } = "Unknown";
            public string Email { get; set; } = "";
            public DateTime AddedAt { get; set; }
            public bool IsCompleted { get; set; }
            public int StudentListId { get; set; }
        }

        [BindProperty]
        public string? EditTitle { get; set; }

        [BindProperty]
        public string? EditDescription { get; set; }

        [BindProperty]
        public string? EditGradeLevel { get; set; }

        public async Task<IActionResult> OnGetAsync(int listId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            ManagedList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);

            if (ManagedList == null)
            {
                TempData["ErrorMessage"] = "List not found or you don't have permission.";
                return RedirectToPage("/Lists/List");
            }

            EditTitle = ManagedList.title;
            EditDescription = ManagedList.description;
            EditGradeLevel = ManagedList.grade_level;

            // Load items with product details
            var items = await _productsContext.Product_list_items
                .Where(i => i.list_ID == listId)
                .ToListAsync();

            foreach (var item in items)
            {
                var product = await _productsContext.Products
                    .FirstOrDefaultAsync(p => p.product_ID == item.product_ID);
                if (product != null)
                    ListItems.Add((item, product));
            }

            // Load students who imported this list
            var studentRecords = await _listContext.PublishedList_Student
                .Where(ps => ps.published_listID == listId)
                .OrderByDescending(ps => ps.added_at)
                .ToListAsync();

            foreach (var record in studentRecords)
            {
                var student = await _userContext.User
                    .FirstOrDefaultAsync(u => u.UserID == record.student_userID);

                Students.Add(new StudentViewModel
                {
                    Name = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                    Email = student?.Email ?? "",
                    AddedAt = record.added_at,
                    IsCompleted = record.is_completed,
                    StudentListId = record.student_listID
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostTogglePublishAsync(int listId)
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
                return RedirectToPage("/Lists/List");
            }

            list.is_published = !list.is_published;
            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = list.is_published
                ? "List published successfully! Students can now find it."
                : "List unpublished. It is no longer visible to students.";

            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostUpdateDetailsAsync(int listId)
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
                return RedirectToPage("/Lists/List");
            }

            list.title = EditTitle;
            list.description = EditDescription;
            list.grade_level = EditGradeLevel;
            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "List details updated.";
            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostUpdateItemAsync(
            int listId, int listItemId, string? teacherNote, bool isRequired, int? recommendedQuantity)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            // Verify ownership
            var list = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);
            if (list == null)
            {
                TempData["ErrorMessage"] = "List not found.";
                return RedirectToPage("/Lists/List");
            }

            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == listItemId && i.list_ID == listId);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage(new { listId });
            }

            item.teacher_note = teacherNote;
            item.is_required = isRequired;
            item.recommended_quantity = recommendedQuantity;
            await _productsContext.SaveChangesAsync();

            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item updated.";
            return RedirectToPage(new { listId });
        }
    }
}