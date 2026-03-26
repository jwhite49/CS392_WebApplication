using Microsoft.AspNetCore.Mvc;
using CS392_WebApplication.Services; //needs this for ProductServices to call

namespace CS392_WebApplication.Controllers
{
    [ApiController] //ASP.net validation, provides automatic error responses 
    [Route("api/[controller]")] //base route for all endpoints in this controller, can be accessed at /api/controller/endpoint
    public class APIController : ControllerBase // "ControllerBase" provides error handling, request data access, and model binding 
    {
        private readonly ProductService _productService; //service field, allows access to product service methods
        // "readonly" means variable can only be assigned once 

        public APIController(ProductService productService) //constructor of controller 
        {
            _productService = productService;
            //stores injected into private field 
            //this needs to be stated so controller can call search and save
        }

        [HttpGet("search")] //defines http method type and endpoint 
        public async Task<IActionResult> Search(string query) //API endpoint 
        {
            //"<IActionResult>" is a flexible return type that can return multiple response types

            var products = await _productService.SearchAndSaveProducts(query); 
            //calls service method to search and save products, waits for result

            return Ok(products); //returns products in response with 200 OK status code
            // "Ok()" is a helper method that creates a response with the specified data and status code, in this case it returns the list of products with a 200 OK status code, indicating the request was successful

        }
    }
}