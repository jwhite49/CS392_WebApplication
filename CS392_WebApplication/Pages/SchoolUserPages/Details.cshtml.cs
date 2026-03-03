using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Pages.SchoolUserPages
{
    public class DetailsModel : PageModel
    {
        private readonly School_UserDbContext _context;

        public DetailsModel(School_UserDbContext context)
        {
            _context = context;
        }

        public School_User School_User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var school_user = await _context.School_User.FirstOrDefaultAsync(m => m.userID == id);
            if (school_user == null)
            {
                return NotFound();
            }
            else
            {
                School_User = school_user;
            }
            return Page();
        }
    }
}
