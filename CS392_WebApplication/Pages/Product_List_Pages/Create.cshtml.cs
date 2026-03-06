using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS392_WebApplication.Pages.Product_List_Pages
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ProductsDbContext _context;
        public IEnumerable<SelectListItem> ListTypeOptions { get; set; }

        public CreateModel(ProductsDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ListTypeOptions = Enum.GetValues(typeof(ListType))
                .Cast<ListType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                });
            return Page();
        }

        [BindProperty]
        public Product_list Product_list { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Product_list.Add(Product_list);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
