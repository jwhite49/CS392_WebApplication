using CS392_WebApplication.API;
using CS392_WebApplication.Data;
using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace CS392_WebApplication.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SeedProductsModel : PageModel
    {
        private readonly ProductsDbContext _productsContext;
        private readonly apiConfig _serpApiService;
        private readonly ILogger<SeedProductsModel> _logger;

        public SeedProductsModel(ProductsDbContext productsContext, apiConfig serpApiService, ILogger<SeedProductsModel> logger)
        {
            _productsContext = productsContext;
            _serpApiService = serpApiService;
            _logger = logger;
        }

        public class SeedResult
        {
            public string Term { get; set; } = "";
            public int Added { get; set; }
            public int Skipped { get; set; }
            public string? Error { get; set; }
        }

        public List<SeedResult> Results { get; set; } = new();
        public int TotalAdded => Results.Sum(r => r.Added);
        public int TotalSkipped => Results.Sum(r => r.Skipped);

        // Curated list of specific school supply terms to seed
        public List<string> SeedTerms { get; } = new()
        {
            // Writing
            "ballpoint pens school", "mechanical pencils", "colored markers", "highlighters",
            "crayons school", "calligraphy pens",
            // Paper
            "notebooks college ruled", "composition notebooks", "sticky notes",
            "printer paper", "construction paper", "graph paper",
            // Organization
            "3 ring binders", "file folders school", "dividers binder", "paper clips",
            "sheet protectors", "desk organizer",
            // Math/Science
            "scientific calculator", "graphing calculator", "geometry set compass ruler",
            "protractor school", "abacus",
            // Art Supplies
            "watercolor paint set", "paint brushes art", "pastels art school",
            "charcoal drawing set", "sketch pad",
            // Study Tools
            "flashcards blank", "index cards", "bookmarks school",
            // Storage
            "pencil case", "school supply box", "storage containers school",
            // Digital
            "USB flash drive school", "stylus tablet school", "headphones school",
            "device case student", "external hard drive student",
            // Presentation
            "poster board school", "dry erase markers", "whiteboard school",
            // Technology
            "laptop backpack student", "tablet school", "laptop school",
            // Basic Essentials
            "water bottle school", "lunch box school", "backpack student",
            "book cover school", "locker organizer",
            // Correction
            "erasers school", "correction tape", "white out",
            // Adhesives
            "glue sticks school", "scotch tape", "rubber cement",
            // Cutting
            "scissors school", "paper cutter", "craft knife",
        };

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Load all existing product names once for dedup check
            var existingNames = (await _productsContext.Products
                .Select(p => p.product_name.ToLower())
                .ToListAsync())
                .ToHashSet();

            foreach (var term in SeedTerms)
            {
                var result = new SeedResult { Term = term };

                try
                {
                    var apiResults = _serpApiService.SearchProducts(term);
                    var toAdd = new List<Products>();

                    foreach (JObject item in apiResults ?? new JArray())
                    {
                        if (toAdd.Count >= 30) break;

                        var name = item["title"]?.ToString()?[..Math.Min(item["title"]?.ToString()?.Length ?? 0, 50)] ?? "";
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        // Skip duplicates
                        if (existingNames.Contains(name.ToLower()))
                        {
                            result.Skipped++;
                            continue;
                        }

                        var product = new Products
                        {
                            product_name = name,
                            description = item["description"]?.ToString()?.Trim() is { Length: > 0 } d ? d : "No description available.",
                            retail_URL = item["serpapi_link"]?.ToString()?.Trim() is { Length: > 0 } u ? u : item["link"]?.ToString() ?? "#",
                            ImageURL = item["thumbnail"]?.ToString(),
                            source_name = item["source"]?.ToString() is { Length: > 0 } sn ? sn[..Math.Min(sn.Length, 40)] : null,
                            source_logo = item["source_icon"]?.ToString() is { Length: > 0 } sl ? sl[..Math.Min(sl.Length, 255)] : null,
                            rating = item["rating"]?.ToObject<double?>(),
                            reviews = item["reviews"]?.ToObject<int?>(),
                            bulk_availability = false,
                            retail_price = double.TryParse(item["extracted_price"]?.ToString(), out double price) ? price : 0,
                        };

                        toAdd.Add(product);
                        existingNames.Add(name.ToLower()); // prevent duplicates within this run
                    }

                    if (toAdd.Any())
                    {
                        _productsContext.Products.AddRange(toAdd);
                        await _productsContext.SaveChangesAsync();
                        result.Added = toAdd.Count;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Seeder failed for term: {Term}", term);
                    result.Error = ex.Message[..Math.Min(ex.Message.Length, 80)];
                }

                Results.Add(result);
            }

            return Page();
        }
    }
}
