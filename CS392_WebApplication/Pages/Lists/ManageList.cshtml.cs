using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;
using System.Security.Cryptography;

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
            public int StudentListId { get; set; }
            public int CompletedItems { get; set; }
            public int TotalItems { get; set; }
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
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            ManagedList = await _listContext.Product_list
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.listID == listId && l.userID == appUser.UserID);

            if (ManagedList == null)
            {
                TempData["ErrorMessage"] = "List not found or you don't have permission.";
                return RedirectToPage("/Lists/List");
            }

            EditTitle = ManagedList.title;
            EditDescription = ManagedList.description;
            EditGradeLevel = ManagedList.grade_level;

            // OPTIMIZED: Bulk load items and products
            var items = await _productsContext.Product_list_items
                .AsNoTracking()
                .Where(i => i.list_ID == listId)
                .ToListAsync();

            if (items.Any())
            {
                var productIds = items.Select(i => i.product_ID).Distinct().ToList();
                var products = await _productsContext.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.product_ID))
                    .ToDictionaryAsync(p => p.product_ID);

                foreach (var item in items)
                {
                    if (products.TryGetValue(item.product_ID, out var product))
                    {
                        ListItems.Add((item, product));
                    }
                }
            }

            // OPTIMIZED: Bulk load student data
            var studentRecords = await _listContext.PublishedList_Student
                .AsNoTracking()
                .Where(ps => ps.published_listID == listId)
                .OrderByDescending(ps => ps.added_at)
                .ToListAsync();

            if (studentRecords.Any())
            {
                // Bulk load all students
                var studentUserIds = studentRecords.Select(sr => sr.student_userID).Distinct().ToList();
                var students = await _userContext.User
                    .AsNoTracking()
                    .Where(u => studentUserIds.Contains(u.UserID))
                    .ToDictionaryAsync(u => u.UserID);

                // Bulk load all student list items
                var studentListIds = studentRecords.Select(sr => sr.student_listID).ToList();
                var allStudentItems = await _productsContext.Product_list_items
                    .AsNoTracking()
                    .Where(i => studentListIds.Contains(i.list_ID))
                    .ToListAsync();

                var itemsByListId = allStudentItems
                    .GroupBy(i => i.list_ID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var record in studentRecords)
                {
                    var studentItems = itemsByListId.GetValueOrDefault(record.student_listID, new List<Product_list_items>());
                    var completedCount = studentItems.Count(i => i.is_purchased);
                    var totalCount = studentItems.Count;

                    var studentUser = students.GetValueOrDefault(record.student_userID);

                    Students.Add(new StudentViewModel
                    {
                        Name = studentUser != null ? $"{studentUser.FirstName} {studentUser.LastName}" : "Unknown",
                        Email = studentUser?.Email ?? "",
                        AddedAt = record.added_at,
                        StudentListId = record.student_listID,
                        CompletedItems = completedCount,
                        TotalItems = totalCount
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdatePublishModeAsync(int listId, int publishMode)
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

            var newMode = (PublishMode)publishMode;
            list.publish_mode = newMode;
            list.is_published = (newMode != PublishMode.None);

            // Generate private code if switching to Private mode
            if (newMode == PublishMode.Private && string.IsNullOrEmpty(list.private_code))
            {
                list.private_code = GeneratePrivateCode();
            }
            // Clear private code if not using Private mode
            else if (newMode != PublishMode.Private)
            {
                list.private_code = null;
            }

            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            var message = newMode switch
            {
                PublishMode.None => "List unpublished. It is now private to you.",
                PublishMode.Public => "List published publicly! Students can browse and add it.",
                PublishMode.Private => $"List published privately! Share code: {list.private_code}",
                _ => "Publishing settings updated."
            };

            TempData["SuccessMessage"] = message;
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
            int listId, int listItemId, string? teacherNote, int requirementLevel, int? recommendedQuantity)
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
            item.requirement_level = (Product_list_items.RequirementLevel)requirementLevel;
            item.recommended_quantity = recommendedQuantity;
            await _productsContext.SaveChangesAsync();

            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item updated.";
            return RedirectToPage(new { listId });
        }

        private string GeneratePrivateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new char[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[8];
                rng.GetBytes(buffer);
                for (int i = 0; i < 8; i++)
                {
                    random[i] = chars[buffer[i] % chars.Length];
                }
            }
            return new string(random);
        }
    }
}