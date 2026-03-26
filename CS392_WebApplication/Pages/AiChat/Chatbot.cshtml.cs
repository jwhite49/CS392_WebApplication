using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CS392_WebApplication.Models;
using CS392_WebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.AiChat
{
    public class ChatbotModel : PageModel
    {
        //private readonly MongoDBService _mongo;
        private readonly ProductsDbContext _context;

        private readonly GeminiService _ai;
        private readonly ILogger<ChatbotModel> _logger;

        public ChatbotModel(GeminiService ai, ILogger<ChatbotModel> logger, ProductsDbContext context)
        {
            //_mongo = mongo;
            _ai = ai;
            _logger = logger;
            _context = context;
        }

        public List<Products> Product { get; private set; } = new();

        [BindProperty]
        public string? SelectedItemId { get; set; }

        [BindProperty]
        public string? UserQuestion { get; set; }

        public string? AIResponse { get; private set; }

        public bool IsProcessing { get; private set; }

        public async Task OnGetAsync()
        {
            Product = await _context.Products.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;
            try
            {
                Product = await _context.Products.ToListAsync();

                if (string.IsNullOrWhiteSpace(UserQuestion))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a question.");
                    return Page();
                }

                Products? selectedProduct = null;
                if (!string.IsNullOrWhiteSpace(SelectedItemId) && int.TryParse(SelectedItemId, out int productId))
                {
                    selectedProduct = await _context.Products.FindAsync(productId);
                }

                AIResponse = await _ai.SendProductAssistantPromptAsync(selectedProduct, UserQuestion, Product);
                return Page();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Chatbot request failed.");
                ModelState.AddModelError(string.Empty, "Failed to get response from AI: " + ex.Message);
                return Page();
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
