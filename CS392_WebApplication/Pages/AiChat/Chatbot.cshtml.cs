using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CS392_WebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CS392_WebApplication.Pages.AiChat
{
    public class ChatbotModel : PageModel
    {
        private readonly MongoDBService _mongo;
        private readonly GeminiService _ai;
        private readonly ILogger<ChatbotModel> _logger;

        public ChatbotModel(GeminiService ai, ILogger<ChatbotModel> logger, MongoDBService mongo)
        {
            _mongo = mongo;
            _ai = ai;
            _logger = logger;
        }

        public Dictionary<string, List<string>> Catalog { get; private set; } = new();

        [BindProperty]
        public string? SelectedCategory { get; set; }

        [BindProperty]
        public string? UserQuestion { get; set; }

        public string? AIResponse { get; private set; }

        public bool IsProcessing { get; private set; }

        public async Task OnGetAsync()
        {
            Catalog = await _mongo.GetCatalogAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;
            try
            {
                Catalog = await _mongo.GetCatalogAsync();

                if (string.IsNullOrWhiteSpace(UserQuestion))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a question.");
                    return Page();
                }

                List<string>? categoryItems = null;
                if (!string.IsNullOrWhiteSpace(SelectedCategory) && Catalog.TryGetValue(SelectedCategory, out var items))
                    categoryItems = items;

                AIResponse = await _ai.SendCategoryAssistantPromptAsync(SelectedCategory, categoryItems, UserQuestion, Catalog);
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
