using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var aiSettings = config.GetSection("AISettings");
            _apiKey = aiSettings["ApiKey"] ?? throw new InvalidOperationException("AISettings:ApiKey is not configured.");
            _model = aiSettings["Model"] ?? "gemini-2.5-flash";
            var baseAddress = aiSettings["BaseAddress"] ?? "https://generativelanguage.googleapis.com/v1beta/";
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        private static string BuildPromptFromMessages(object[] messages)
        {
            var sb = new StringBuilder();
            foreach (var msg in messages)
            {
                var roleProp = msg.GetType().GetProperty("role")?.GetValue(msg)?.ToString();
                var contentProp = msg.GetType().GetProperty("content")?.GetValue(msg)?.ToString();
                if (!string.IsNullOrEmpty(roleProp))
                {
                    sb.Append($"[{roleProp}] ");
                }
                if (!string.IsNullOrEmpty(contentProp))
                {
                    sb.AppendLine(contentProp);
                }
            }
            return sb.ToString();
        }

        private static string ExtractGeminiText(string respText, ILogger logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(respText);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return text ?? string.Empty;
                    }
                }
                logger.LogError("Gemini API returned unexpected JSON shape: {Response}", respText);
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse Gemini response JSON: {Response}", respText);
                throw;
            }
        }

        public async Task<string> SendProductSummaryAsync(Products prod)
        {
            if (prod == null) throw new ArgumentNullException(nameof(prod));

            var userPrompt = new[]
            {
                new { role = "system", content = "You are a helpful shopping assistant. Provide a brief, friendly summary of the product." },
                new { role = "user", content = $"Summarize this product for a customer: {prod.product_name} - {prod.description}. Price: ${prod.retail_price:F2}. Rating: {prod.rating?.ToString("F1") ?? "N/A"} stars." }
            };

            var promptText = BuildPromptFromMessages(userPrompt);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        public async Task<string> AskGeminiAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentNullException(nameof(prompt));

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        public async Task<string> SendProductAssistantPromptAsync(Products? selectedProduct, string userQuestion, List<Products>? allProducts = null)
        {
            if (string.IsNullOrWhiteSpace(userQuestion)) throw new ArgumentNullException(nameof(userQuestion));

            var systemPrompt = @"You are a helpful shopping assistant for a school supplies catalog. 
Your role is to help customers find the right products, answer questions about product features, 
prices, ratings, and provide recommendations. Be friendly, concise, and helpful.
If a specific product is provided, focus your answer on that product.
If no specific product is selected, provide general guidance based on the user's question.";

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("[system] " + systemPrompt);
            contextBuilder.AppendLine();
            
            if (selectedProduct != null)
            {
                contextBuilder.AppendLine("[context] Selected Product:");
                contextBuilder.AppendLine($"- Name: {selectedProduct.product_name}");
                contextBuilder.AppendLine($"- Description: {selectedProduct.description}");
                contextBuilder.AppendLine($"- Price: ${selectedProduct.retail_price:F2}");
                if (selectedProduct.rating.HasValue)
                    contextBuilder.AppendLine($"- Rating: {selectedProduct.rating.Value:F1} stars");
                if (selectedProduct.reviews.HasValue)
                    contextBuilder.AppendLine($"- Reviews: {selectedProduct.reviews.Value}");
                contextBuilder.AppendLine($"- Source: {selectedProduct.source_name}");
                contextBuilder.AppendLine($"- Bulk Available: {(selectedProduct.bulk_availability ? "Yes" : "No")}");
                contextBuilder.AppendLine();
            }
            else if (allProducts != null && allProducts.Count > 0)
            {
                contextBuilder.AppendLine("[context] Available product categories in our catalog:");
                var sampleProducts = allProducts.Take(10).Select(p => $"- {p.product_name} (${p.retail_price:F2})");
                contextBuilder.AppendLine(string.Join("\n", sampleProducts));
                contextBuilder.AppendLine($"... and {allProducts.Count} total products in catalog");
                contextBuilder.AppendLine();
            }
            
            contextBuilder.AppendLine($"[user] {userQuestion}");

            var messages = new[]
            {
                new { role = "user", content = contextBuilder.ToString() }
            };

            var promptText = BuildPromptFromMessages(messages);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }
    }
}