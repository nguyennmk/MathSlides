using MathSlides.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MathSlides.Service.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrlRelative;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? "AIzaSyD2XcypDUT6GTueUgybNHotbFJNJkcAsvw";

            if (_apiKey == "AIzaSyD2XcypDUT6GTueUgybNHotbFJNJkcAsvw")
            {
                Console.WriteLine("Warning: Đang sử dụng API Key mẫu.");
            }

            _apiUrlRelative = $"v1beta/models/gemini-2.5-flash-preview-09-2025:generateContent?key={_apiKey}";
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                safetySettings = new[]
                {
                    new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                }
            };

            int maxRetries = 3;
            int delay = 1000;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_apiUrlRelative, payload);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<JsonNode>();
                        if (result?["candidates"] == null) return string.Empty;

                        var generatedText = result?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();

                        if (generatedText != null && generatedText.StartsWith("```json"))
                        {
                            generatedText = generatedText.Trim('`', 'j', 's', 'o', 'n', '\n', '\r', ' ');
                        }

                        // === SỬA LỖI JSON (Cách Mới) ===
                        // Gemini trả về (ví dụ): "\frac{a}{b}"
                        // Chúng ta cần: "\\frac{a}{b}"
                        if (generatedText != null)
                        {
                            generatedText = generatedText.Replace(@"\", @"\\");
                        }
                        // ===============================

                        return generatedText ?? string.Empty;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode == 429)
                    {
                        await Task.Delay(delay);
                        delay *= 2;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Gemini API request failed with status code {response.StatusCode}: {errorContent}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Gemini API request failed (attempt {i + 1}): {ex.Message}. Retrying in {delay / 1000}s...");
                    if (i == maxRetries - 1) throw;
                    await Task.Delay(delay);
                    delay *= 2;
                }
            }
            return string.Empty;
        }
    }
}