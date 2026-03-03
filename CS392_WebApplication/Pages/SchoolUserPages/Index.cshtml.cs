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
    public class IndexModel : PageModel
    {
        private readonly School_UserDbContext _context;

        public IndexModel(School_UserDbContext context)
        {
            _context = context;
        }

        public IList<School_User> School_User { get;set; } = default!;

        public async Task OnGetAsync()
        {
            School_User = await _context.School_User.ToListAsync();
        }
    }
}
