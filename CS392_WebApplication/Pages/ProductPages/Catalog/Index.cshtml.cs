using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.ProductPages.Catalog
{
    public class IndexModel : PageModel
    {
        private readonly ProductsDbContext _context;
        //Variable for sorting
        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; }
        public IndexModel(ProductsDbContext context)
        {
            _context = context;
        }

        public IList<Products> Products { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Products.AsQueryable();

            // Search filter for search bar
            if (!string.IsNullOrEmpty(Search))
            {
                query = query.Where(p =>
                    p.product_name.Contains(Search) ||
                    p.description.Contains(Search));
            }

            // Sorting for sorting options
            query = Sort switch
            {
                "name_asc" => query.OrderBy(p => p.product_name),
                "name_desc" => query.OrderByDescending(p => p.product_name),
                "price_asc" => query.OrderBy(p => p.retail_price),
                "price_desc" => query.OrderByDescending(p => p.retail_price),
                _ => query.OrderBy(p => p.product_name) // default sort
            };

            Products = await query.ToListAsync();
        }

    }
}
