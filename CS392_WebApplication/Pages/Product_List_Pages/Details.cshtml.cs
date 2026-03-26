using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS392_WebApplication.Pages.Product_List_Pages
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ProductsDbContext _context;

        public DetailsModel(ProductsDbContext context)
        {
            _context = context;
        }

        public Product_list Product_list { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product_list = await _context.Product_list.FirstOrDefaultAsync(m => m.listID == id);
            if (product_list == null)
            {
                return NotFound();
            }
            else
            {
                Product_list = product_list;
            }
            return Page();
        }
    }
}
