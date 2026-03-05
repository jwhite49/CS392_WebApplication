using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security;

namespace CS392_WebApplication.Pages.ProductPages.Listing
{
    public class ProductListingModel : PageModel
    {
        private readonly ProductsDbContext _context;
        private readonly ILogger<ProductListingModel> _logger;

        public ProductListingModel(ILogger<ProductListingModel> logger, ProductsDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IList<Products> Products { get; set; } = default!;
        public Products Product { get; set; } = default!; //single product instead of a list
       
        public async Task<IActionResult> OnGetAsync(string category)//takes category whichb is passed in the query string and filters products based on that category
        {

            //checks if there is data to be passed to search for product
            //Needs to be revisited
            if (String.IsNullOrWhiteSpace(category))
            {
                _logger.LogWarning("Error finding category: {category}", category);
                return NotFound();
            }
            else
            {
               Products = await _context.Products
                    .Where(p => p.product_name.Contains(category/* StringComparison.OrdinalIgnoreCase*/))
                    .ToListAsync();
                    //ordinalIgnoreCase does not check for case senssitivity
               Product = Products.FirstOrDefault(); //get the first product from the list of products
               return Page();
            } 
     


        }
    }

}
