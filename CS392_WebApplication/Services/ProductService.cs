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
                    description = result["description"]?.ToString(),
                    retail_URL = result["serpapi_link"]?.ToString(),
                    retail_price = result["extracted_price"].ToObject<double>(),
                    ImageURL = result["thumbnail"]?.ToString(),
                    source_name = result["source"]?.ToString(),
                    source_logo = result["source_logo"]?.ToString(),
                    rating = result["rating"] != null ? result["rating"].ToObject<double>() : 0.00,
                    reviews = result["reviews"]?.ToObject<int?>(),
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