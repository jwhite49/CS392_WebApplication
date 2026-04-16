using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;
using System.ComponentModel.DataAnnotations;

namespace CS392_WebApplication.Pages.Lists
{
    [Authorize]
    public class CreateListModel : PageModel
    {
        private readonly Product_listDbContext _listContext;
        private readonly UserDbContext _userContext;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateListModel(
            Product_listDbContext listContext,
            UserDbContext userContext,
            UserManager<IdentityUser> userManager)
        {
            _listContext = listContext;
            _userContext = userContext;
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a title for your list")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [MaxLength(500)]
        public string? Description { get; set; }

        [BindProperty]
        [Range(0.01, 999999.99, ErrorMessage = "Budget must be greater than $0")]
        public double? BudgetAmount { get; set; }

        [BindProperty]
        [MaxLength(100)]
        public string? ListCategory { get; set; }

        // Predefined category options
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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            var newList = new Product_list
            {
                userID = appUser.UserID,
                title = Title,
                description = Description,
                total_price = 0,
                list_type = ListType.User, // Student-created list
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                is_published = false,
                publish_mode = PublishMode.None,
                budget_amount = BudgetAmount,
                list_category = ListCategory
            };

            _listContext.Product_list.Add(newList);
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "List created successfully!";
            return RedirectToPage("/Lists/ListDetails", new { listId = newList.listID }); // Fixed: id -> listId
        }
    }
}