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
    public class ViewStudentProgressModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly ProductsDbContext _productsContext;
        private readonly UserManager<IdentityUser> _userManager;

        public ViewStudentProgressModel(
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

        public string StudentName { get; set; } = "Unknown";
        public string StudentEmail { get; set; } = "";
        public DateTime AddedDate { get; set; }
        public string ListTitle { get; set; } = "Unknown List";
        public double TotalPrice { get; set; }
        public int PublishedListId { get; set; }
        public int CompletedItems { get; set; }
        public int TotalItems { get; set; }
        public int ProgressPercent => TotalItems > 0 ? (int)((CompletedItems * 100.0) / TotalItems) : 0;

        public List<ItemViewModel> Items { get; set; } = new();

        public class ItemViewModel
        {
            public string ProductName { get; set; } = "";
            public bool IsPurchased { get; set; }
            public Product_list_items.RequirementLevel RequirementLevel { get; set; }
            public int Quantity { get; set; }
            public float Price { get; set; }
            public string? TeacherNote { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int studentListId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            // Find the import record
            var importRecord = await _listContext.PublishedList_Student
                .FirstOrDefaultAsync(ps => ps.student_listID == studentListId);

            if (importRecord == null)
            {
                TempData["ErrorMessage"] = "Student record not found.";
                return RedirectToPage("/Lists/List");
            }

            // Verify teacher owns the published list
            var publishedList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.listID == importRecord.published_listID && l.userID == appUser.UserID);

            if (publishedList == null)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this student's progress.";
                return RedirectToPage("/Lists/List");
            }

            PublishedListId = publishedList.listID;
            ListTitle = publishedList.title ?? "Unknown List";
            TotalPrice = publishedList.total_price;
            AddedDate = importRecord.added_at;

            // Get student info
            var student = await _userContext.User
                .FirstOrDefaultAsync(u => u.UserID == importRecord.student_userID);
            if (student != null)
            {
                StudentName = $"{student.FirstName} {student.LastName}";
                StudentEmail = student.Email ?? "";
            }

            // Get student's items
            var studentItems = await _productsContext.Product_list_items
                .Where(i => i.list_ID == studentListId)
                .ToListAsync();

            TotalItems = studentItems.Count;
            CompletedItems = studentItems.Count(i => i.is_purchased);

            foreach (var item in studentItems)
            {
                var product = await _productsContext.Products
                    .FirstOrDefaultAsync(p => p.product_ID == item.product_ID);

                Items.Add(new ItemViewModel
                {
                    ProductName = product?.product_name ?? "Unknown Product",
                    IsPurchased = item.is_purchased,
                    RequirementLevel = item.requirement_level,
                    Quantity = item.quantity,
                    Price = item.price_at_purchase,
                    TeacherNote = item.teacher_note
                });
            }

            return Page();
        }
    }
}