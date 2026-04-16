using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;
using System.ComponentModel.DataAnnotations;

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
        public bool IsImportedList => ImportRecord != null;

        // Can modify list structure (add/remove items) - only for non-imported user lists
        public bool CanModifyList => !IsImportedList;

        // Budget tracking properties (only for non-imported user lists)
        public bool ShowBudgetFeatures => !IsImportedList && UserList.list_type == ListType.User;
        
        // Progress tracking for imported lists
        public bool ShowProgressTracking => IsImportedList;
        public double TotalSpent { get; set; }
        public double RemainingToBuy { get; set; }
        public int PurchasedItemsCount { get; set; }
        public int TotalItemsCount { get; set; }
        public double AverageItemPrice { get; set; }
        public double MostExpensiveItemPrice { get; set; }

        [BindProperty]
        [Range(0.01, 999999.99, ErrorMessage = "Budget must be greater than $0")]
        public double? EditBudgetAmount { get; set; }

        [BindProperty]
        [MaxLength(100)]
        public string? EditListCategory { get; set; }

        public List<string> CategoryOptions { get; } = new()
        {
            "Elementary School Supplies",
            "Middle School Supplies",
            "High School Supplies",
            "College - Dorm Essentials",
            "College - Class Materials",
            "Art & Craft Supplies",
            "Sports Equipment",
            "Birthday Wishlist",
            "Holiday Shopping",
            "Other"
        };

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

            // Check if this list was imported
            ImportRecord = await _listContext.PublishedList_Student
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

            // Load list items with products
            var items = await _productsContext.Product_list_items
                .Where(i => i.list_ID == listId)
                .ToListAsync();

            if (items.Any())
            {
                var productIds = items.Select(i => i.product_ID).Distinct().ToList();
                var products = await _productsContext.Products
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

            // Calculate statistics for both imported and user lists
            if (ListItems.Any())
            {
                TotalSpent = ListItems.Where(x => x.Item.is_purchased).Sum(x => x.Product.retail_price * x.Item.quantity);
                RemainingToBuy = ListItems.Where(x => !x.Item.is_purchased).Sum(x => x.Product.retail_price * x.Item.quantity);
                PurchasedItemsCount = ListItems.Count(x => x.Item.is_purchased);
                TotalItemsCount = ListItems.Count;
                AverageItemPrice = ListItems.Average(x => x.Product.retail_price);
                MostExpensiveItemPrice = ListItems.Max(x => x.Product.retail_price);
            }

            // Set edit values
            EditBudgetAmount = UserList.budget_amount;
            EditListCategory = UserList.list_category;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateBudgetAsync(int listId)
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

            // Check if imported (shouldn't be able to edit budget)
            var importRecord = await _listContext.PublishedList_Student
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

            if (importRecord != null)
            {
                TempData["ErrorMessage"] = "Cannot edit budget for imported lists.";
                return RedirectToPage(new { listId });
            }

            list.budget_amount = EditBudgetAmount;
            list.list_category = EditListCategory;
            list.updated_at = DateTime.UtcNow;

            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Budget and category updated successfully!";
            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostTogglePurchasedAsync(int listId, int itemId)
        {
            // Allow toggle for both imported and user-created lists
            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == itemId && i.list_ID == listId);

            if (item != null)
            {
                item.is_purchased = !item.is_purchased;
                await _productsContext.SaveChangesAsync();

                // Recalculate total_price
                await RecalculateTotalPrice(listId);

                if (item.is_purchased)
                {
                    TempData["SuccessMessage"] = "Item marked as purchased!";
                }
            }

            return RedirectToPage(new { listId });
        }

        public async Task<IActionResult> OnPostRemoveItemAsync(int listId, int itemId)
        {
            // Check if this is an imported list (can't remove items)
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            var importRecord = await _listContext.PublishedList_Student
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.student_listID == listId && ps.student_userID == appUser.UserID);

            if (importRecord != null)
            {
                TempData["ErrorMessage"] = "Cannot remove items from imported lists.";
                return RedirectToPage(new { listId });
            }

            var item = await _productsContext.Product_list_items
                .FirstOrDefaultAsync(i => i.list_items_ID == itemId && i.list_ID == listId);

            if (item != null)
            {
                _productsContext.Product_list_items.Remove(item);
                await _productsContext.SaveChangesAsync();

                await RecalculateTotalPrice(listId);
                TempData["SuccessMessage"] = "Item removed from list.";
            }

            return RedirectToPage(new { listId });
        }

        private async Task RecalculateTotalPrice(int listId)
        {
            var list = await _listContext.Product_list.FirstOrDefaultAsync(l => l.listID == listId);
            if (list == null) return;

            var items = await _productsContext.Product_list_items
                .Where(i => i.list_ID == listId)
                .ToListAsync();

            if (items.Any())
            {
                var productIds = items.Select(i => i.product_ID).ToList();
                var products = await _productsContext.Products
                    .Where(p => productIds.Contains(p.product_ID))
                    .ToDictionaryAsync(p => p.product_ID);

                list.total_price = items.Sum(i => products.TryGetValue(i.product_ID, out var p) ? p.retail_price * i.quantity : 0);
            }
            else
            {
                list.total_price = 0;
            }

            list.updated_at = DateTime.UtcNow;
            await _listContext.SaveChangesAsync();
        }
    }
}