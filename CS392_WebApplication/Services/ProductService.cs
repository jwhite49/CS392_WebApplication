using CS392_WebApplication.Data;
using CS392_WebApplication.API;
using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Microsoft.AspNetCore.Razor.Language;


namespace CS392_WebApplication.Services
{
    public class ProductService
    {
        private readonly ProductsDbContext _context; //database field
        private readonly apiConfig _serpApiService; // api service field

        public ProductService(ProductsDbContext context, apiConfig serpApiService)
        {
            _context = context;
            _serpApiService = serpApiService;
        }

        public async Task<List<Products>> SearchAndSaveProducts(string query)
        {
            var results = _serpApiService.SearchProducts(query); //calls serp api
            var products = new List<Products>(); //creates empty list to hold converted product objects

            foreach (JObject result in results) //loops through API json results
            {
                var product = new Products //convert json to product object
                {
                    product_name = result["title"]?.ToString(), //"?" prevents errors if value is missing
                    description = result["snippet"]?.ToString() ?? "No description available.",
                    retail_URL = result["link"]?.ToString()?.Trim() is { Length: > 0 } u1 ? u1
                        : result["serpapi_link"]?.ToString()?.Trim() is { Length: > 0 } u2 ? u2 : "#",
                    retail_price = double.TryParse(result["extracted_price"]?.ToString(), out double p) ? p : 0,
                    ImageURL = result["thumbnail"]?.ToString(),
                    source_name = result["source"]?.ToString(),
                    source_logo = result["source_icon"]?.ToString(),
                    rating = result["rating"]?.ToObject<double?>(),
                    reviews = result["reviews"]?.ToObject<int?>(),
                    google_product_id = result["product_id"]?.ToString(),
                };
                products.Add(product); //add converted product to list
            }
            _context.Products.AddRange(products); // add all products to database, not saved yet 
            await _context.SaveChangesAsync(); //save changes to database, now products are stored in DB, executes sql line
            // await allows the database operation to run asynchronously
            return products; //returns saved products back to controller
        }
    }
}