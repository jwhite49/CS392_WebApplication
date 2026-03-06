using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS392_WebApplication.Pages.Product_List_Pages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ProductsDbContext _context;
        public IEnumerable<SelectListItem> ListTypeOptions { get; set; }

        public EditModel(ProductsDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product_list Product_list { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ListTypeOptions = Enum.GetValues(typeof(ListType))
                .Cast<ListType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                });

            if (id == null)
            {
                return NotFound();
            }

            var product_list =  await _context.Product_list.FirstOrDefaultAsync(m => m.listID == id);
            if (product_list == null)
            {
                return NotFound();
            }
            Product_list = product_list;
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

            _context.Attach(Product_list).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Product_listExists(Product_list.listID))
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

        private bool Product_listExists(int id)
        {
            return _context.Product_list.Any(e => e.listID == id);
        }
    }
}
