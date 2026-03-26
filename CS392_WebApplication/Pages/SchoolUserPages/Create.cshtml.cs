using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Pages.SchoolUserPages
{
    public class CreateModel : PageModel
    {
        private readonly School_UserDbContext _context;

        public CreateModel(School_UserDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public School_User School_User { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.School_User.Add(School_User);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
