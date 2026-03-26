using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Pages.SchoolUserPages
{
    public class EditModel : PageModel
    {
        private readonly School_UserDbContext _context;

        public EditModel(School_UserDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public School_User School_User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var school_user =  await _context.School_User.FirstOrDefaultAsync(m => m.userID == id);
            if (school_user == null)
            {
                return NotFound();
            }
            School_User = school_user;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(School_User).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!School_UserExists(School_User.userID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool School_UserExists(int id)
        {
            return _context.School_User.Any(e => e.userID == id);
        }
    }
}
