using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CS392_WebApplication.Data;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Pages
{
    public class LoginModel : PageModel
    {
        private readonly UserDbContext _context;

        public LoginModel(UserDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty; /* "asp-for" binds to this */

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Called when the page is loaded
        }

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                // Query the database for the user
                var user = _context.User
                    .FirstOrDefault(u => u.Username == Username && u.Password == Password);

                if (user != null)
                {
                    // Login successful - redirect to home page
                    return RedirectToPage("/Index");
                }
                else
                {
                    // Invalid credentials
                    ErrorMessage = "Invalid username or password.";
                }
            }

            // Return to login page with errors
            return Page();
        }
    }
}