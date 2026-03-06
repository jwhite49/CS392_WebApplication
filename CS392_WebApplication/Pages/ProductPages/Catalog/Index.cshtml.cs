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
        
        public IndexModel(ProductsDbContext context)
        {
            _context = context;
        }

        public IList<Products> Products { get; set; }
        //Variable for sorting
        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; }

        //Search variable for search bar
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }
        //Variables for pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; } = 12; //12 products per page, can be adjusted as needed
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

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

            // Role-based filtering, User cannot see bulk available products.
            if (!User.IsInRole("Admin") && !User.IsInRole("School"))
            {
                // Regular users only see non-bulk products
                query = query.Where(p => p.bulk_availability == false);
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

            // Clamp PageNumber (in case someone passes 0 or negative)
            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            // Total items AFTER filters
            TotalItems = await query.CountAsync();

            // Clamp PageNumber to TotalPages (if there are items)
            if (TotalItems > 0 && PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            //  Apply pagination
            Products = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

    }
}
