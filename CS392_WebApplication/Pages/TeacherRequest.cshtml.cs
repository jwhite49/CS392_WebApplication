using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CS392_WebApplication.Pages
{
    [Authorize]
    public class TeacherRequestModel : PageModel
    {
        private readonly UserDbContext _userContext;
        private readonly School_UserDbContext _schoolUserContext;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        [Required(ErrorMessage = "School name is required.")]
        public string SchoolName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "School email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string SchoolEmail { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Grade is required.")]
        public string Grade { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "School district is required.")]
        public string SchoolDistrict { get; set; }

        public TeacherRequestModel(UserDbContext userContext, School_UserDbContext schoolUserContext, UserManager<IdentityUser> userManager)
        {
            _userContext = userContext;
            _schoolUserContext = schoolUserContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Redirect if already has a request or is already a school user
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Index");

            var dbUser = await _userContext.User.FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (dbUser == null) return RedirectToPage("/Index");

            var existing = await _schoolUserContext.School_User.AnyAsync(s => s.userID == dbUser.UserID);
            if (existing) return RedirectToPage("/Index");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToPage("/Index");

            var dbUser = await _userContext.User.FirstOrDefaultAsync(u => u.Email == identityUser.Email);
            if (dbUser == null) return RedirectToPage("/Index");

            var existing = await _schoolUserContext.School_User.AnyAsync(s => s.userID == dbUser.UserID);
            if (existing) return RedirectToPage("/Index");

            var schoolUser = new School_User
            {
                userID = dbUser.UserID,
                school_name = SchoolName,
                school_email = SchoolEmail,
                grade = Grade,
                school_district = SchoolDistrict,
                is_approved = false
            };

            _schoolUserContext.School_User.Add(schoolUser);
            await _schoolUserContext.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
