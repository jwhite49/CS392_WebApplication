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

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 12; // 12 lists per page
        public int TotalCount { get; set; }

        public class PublishedListViewModel
        {
            public Product_list List { get; set; } = default!;
            public string OwnerName { get; set; } = "Unknown";
            public int ItemCount { get; set; }
            public int StudentCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            CurrentPage = pageNumber < 1 ? 1 : pageNumber;

            // Get total count for pagination
            TotalCount = await _listContext.Product_list
                .AsNoTracking()
                .CountAsync(l => l.is_published && l.publish_mode == PublishMode.Public);

            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Ensure current page is valid
            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;

            // Get only PUBLIC published lists with pagination
            var publishedLists = await _listContext.Product_list
                .AsNoTracking()
                .Where(l => l.is_published && l.publish_mode == PublishMode.Public)
                .OrderByDescending(l => l.updated_at)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Find which ones the current user already added
            var userImports = await _listContext.PublishedList_Student
                .AsNoTracking()
                .Where(ps => ps.student_userID == appUser.UserID)
                .Select(ps => ps.published_listID)
                .ToListAsync();

            AlreadyAddedListIds = new HashSet<int>(userImports);

            if (publishedLists.Any())
            {
                // OPTIMIZED: Bulk load all owners
                var ownerIds = publishedLists.Select(l => l.userID).Distinct().ToList();
                var owners = await _userContext.User
                    .AsNoTracking()
                    .Where(u => ownerIds.Contains(u.UserID))
                    .ToDictionaryAsync(u => u.UserID);

                // OPTIMIZED: Bulk load all item counts
                var listIds = publishedLists.Select(l => l.listID).ToList();
                var itemCounts = await _productsContext.Product_list_items
                    .AsNoTracking()
                    .Where(i => listIds.Contains(i.list_ID))
                    .GroupBy(i => i.list_ID)
                    .Select(g => new { ListId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ListId, x => x.Count);

                // OPTIMIZED: Bulk load all student counts
                var studentCounts = await _listContext.PublishedList_Student
                    .AsNoTracking()
                    .Where(ps => listIds.Contains(ps.published_listID))
                    .GroupBy(ps => ps.published_listID)
                    .Select(g => new { ListId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ListId, x => x.Count);

                foreach (var list in publishedLists)
                {
                    var owner = owners.GetValueOrDefault(list.userID);
                    var itemCount = itemCounts.GetValueOrDefault(list.listID, 0);
                    var studentCount = studentCounts.GetValueOrDefault(list.listID, 0);

                    PublishedLists.Add(new PublishedListViewModel
                    {
                        List = list,
                        OwnerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown",
                        ItemCount = itemCount,
                        StudentCount = studentCount
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostImportByCodeAsync(string privateCode)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (appUser == null) return RedirectToPage("/Error");

            if (string.IsNullOrWhiteSpace(privateCode))
            {
                TempData["ErrorMessage"] = "Please enter a valid private code.";
                return RedirectToPage();
            }

            // Find list by private code
            var originalList = await _listContext.Product_list
                .FirstOrDefaultAsync(l => l.private_code == privateCode.ToUpper().Trim() 
                                       && l.publish_mode == PublishMode.Private
                                       && l.is_published);

            if (originalList == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired private code. Please check and try again.";
                return RedirectToPage();
            }

            // Check if already added
            var alreadyAdded = await _listContext.PublishedList_Student
                .AnyAsync(ps => ps.published_listID == originalList.listID
                             && ps.student_userID == appUser.UserID);

            if (alreadyAdded)
            {
                TempData["ErrorMessage"] = "You have already imported this list.";
                return RedirectToPage();
            }

            // Import the list
            await ImportListForStudent(originalList, appUser);

            TempData["SuccessMessage"] = $"List '{originalList.title}' successfully imported!";
            return RedirectToPage();
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
                .FirstOrDefaultAsync(l => l.listID == publishedListId 
                                       && l.is_published 
                                       && l.publish_mode == PublishMode.Public);

            if (originalList == null)
            {
                TempData["ErrorMessage"] = "Published list not found.";
                return RedirectToPage();
            }

            await ImportListForStudent(originalList, appUser);

            TempData["SuccessMessage"] = "List added to your lists!";
            return RedirectToPage();
        }

        private async Task ImportListForStudent(Product_list originalList, User appUser)
        {
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
                publish_mode = PublishMode.None,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _listContext.Product_list.Add(studentList);
            await _listContext.SaveChangesAsync();

            // Copy all items
            var originalItems = await _productsContext.Product_list_items
                .AsNoTracking()
                .Where(i => i.list_ID == originalList.listID)
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
                    requirement_level = item.requirement_level,
                    recommended_quantity = item.recommended_quantity,
                    teacher_note = item.teacher_note,
                    is_purchased = false
                };
                _productsContext.Product_list_items.Add(copy);
            }
            await _productsContext.SaveChangesAsync();

            // Create the published-student link
            var link = new PublishedList_Student
            {
                published_listID = originalList.listID,
                student_listID = studentList.listID,
                student_userID = appUser.UserID,
                added_at = DateTime.UtcNow,
                is_completed = false
            };

            _listContext.PublishedList_Student.Add(link);
            await _listContext.SaveChangesAsync();
        }
    }
}