using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using CS392_WebApplication.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SerpApi;
using Newtonsoft.Json.Linq;


namespace CS392_WebApplication.Pages.ProductPages.Catalog
{
    public class IndexModel : PageModel
    {
        private readonly ProductsDbContext _context;
        private readonly apiConfig _serpApiService; // api service field
        private readonly catalogListModel allowedSearches; 
        //pulls search data from other page

        
        public IndexModel(ProductsDbContext context, apiConfig serpApiService)
        {
            _context = context;
            _serpApiService = serpApiService;
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

            var apiProducts = new List<Products>();

            bool searchMatch = catalogListModel.AllowedSearches.Any(s => 
            Search != null && (
                Search.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                s.Contains(Search, StringComparison.OrdinalIgnoreCase)
            ));
           //pulls search match from catalogList and checks if filtered search matches user search
           //This limits user searching up explicit items and saving into DB

            if (!string.IsNullOrEmpty(Search) && searchMatch)
            {
                var results = _serpApiService.SearchProducts(Search); //calls serp api 
                
                //currently works and displays results in console, next step is to save results to DB and display on frontend
                var resultLimit = 0;
                foreach (JObject result in results?? new JArray())
                {
                    if(resultLimit >= 20)
                    {
                       break; //limit to 15 results to prevent overloading DB, can be adjusted as needed
                    } 
                var product = new Products //convert json to product object
                {
                    product_name = result["title"]?.ToString()?[..Math.Min(result["title"]?.ToString()?.Length ?? 0, 50)] ?? "",
                    description = result["description"]?.ToString() ?? "",
                    retail_URL = result["serpapi_link"]?.ToString() ?? "",
                    ImageURL = result["thumbnail"]?.ToString() ?? "",
                    source_name = result["source"]?.ToString(),
                    source_logo = result["source_icon"]?.ToString(),
                    rating = result["rating"]?.ToObject<double?>(),
                    reviews = result["reviews"]?.ToObject<int?>(),
                };  
                if(double.TryParse(result["extracted_price"]?.ToString(), out double retailPrice)) 
                { 
                    product.retail_price=retailPrice;
                }
                 else
                {
                    product.retail_price = 0; //default price if conversion fails
                }   
                apiProducts.Add(product); //add converted product to list
                //loop extracts each result
                resultLimit++;
                }
                //saves info after the loop iteration, API gets results into db then application picks up results
            _context.Products.AddRange(apiProducts); // add all products to database, not saved yet 
            await _context.SaveChangesAsync(); //save changes to database, now products are stored in DB, executes sql line

                // Remove duplicate products — keep the oldest entry (lowest ID) for each product_name
                var allProducts = await _context.Products.ToListAsync();
                var duplicates = allProducts
                    .GroupBy(p => p.product_name)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderBy(p => p.product_ID).Skip(1)) // skip the first (oldest), select the rest
                    .ToList();

                if (duplicates.Any())
                {
                    _context.Products.RemoveRange(duplicates);
                    await _context.SaveChangesAsync();
                }
            }
            

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

            //api
            
            
            
        }
    }
}
