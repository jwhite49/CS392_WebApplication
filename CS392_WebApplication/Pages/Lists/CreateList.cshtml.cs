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
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [MaxLength(500)]
        public string? Description { get; set; }

        [BindProperty]
        public string? GradeLevel { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var appUser = await _userContext.User
                .FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null)
                return RedirectToPage("/Error");

            var isSchoolOrAdmin = User.IsInRole("Admin") || User.IsInRole("School");

            var newList = new Product_list
            {
                userID = appUser.UserID,
                title = Title,
                description = Description,
                grade_level = GradeLevel,
                total_price = 0,
                list_type = isSchoolOrAdmin ? ListType.School : ListType.User,
                is_published = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _listContext.Product_list.Add(newList);
            await _listContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "List created successfully!";
            return RedirectToPage("/Lists/List");
        }
    }
}