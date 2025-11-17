using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using MathSlides.Service.DTOs.Generation;
using System.Text;

namespace MathSlides.Service.Services
{
    public class SlideGenerationService : ISlideGenerationService
    {
        #region Constructor, Fields, DTOs
        private readonly IGeminiService _geminiService;
        private readonly IPowerpointService _powerpointService;
        private readonly ITopicRepository _topicRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger<SlideGenerationService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IGDPTService _gdptService;

        private class GeminiContentResponse
        {
            [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
            [JsonPropertyName("formula")] public string Formula { get; set; } = string.Empty;
            [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
            [JsonPropertyName("example")] public string Example { get; set; } = string.Empty;
            [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
        }

        private class GeminiFormula { }
        private class GeminiExample { }

        public SlideGenerationService(
            IGeminiService geminiService,
            IPowerpointService powerpointService,
            ITopicRepository topicRepository,
            IContentRepository contentRepository,
            ILogger<SlideGenerationService> logger,
            IWebHostEnvironment env,
            IGDPTService gdptService)
        {
            _geminiService = geminiService;
            _powerpointService = powerpointService;
            _topicRepository = topicRepository;
            _contentRepository = contentRepository;
            _logger = logger;
            _env = env;
            _gdptService = gdptService;
        }
        #endregion

        private async Task<List<Content>> GetOrGenerateContentAsync(Topic topic, GenerationRequest request)
        {
            _logger.LogInformation("Dữ liệu được cung cấp trực tiếp trong request. Gọi Gemini để định dạng và tạo thêm nội dung.");

            string jsonFormatPrompt = @"Hãy trả về một chuỗi JSON duy nhất (một dòng, không ngắt dòng) là một MẢNG (array) chứa CHÍNH XÁC 2 object.
Mỗi object đại diện cho một slide và phải có các khóa: ""title"", ""summary"", ""formula"", ""example"", ""explanation"", ""source"".
- Object 1: Dành cho Slide 2. Hãy điền thông tin từ dữ liệu gốc tôi cung cấp.
- Object 2: Dành cho Slide 3. Hãy TỰ TẠO nội dung, công thức, ví dụ... liên quan đến chủ đề (ví dụ: 'Phép cộng phân số KHÁC mẫu số' hoặc 'Bài tập vận dụng').";

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Hãy đóng vai trò là một chuyên gia biên soạn sách giáo khoa Toán học Việt Nam.");
            promptBuilder.AppendLine($"Chủ đề chính là: '{request.Name}' (Lớp {topic.Class.Name})."); // Sửa: request.Name
            promptBuilder.AppendLine("Dưới đây là DỮ LIỆU GỐC cho Slide 2:");
            promptBuilder.AppendLine($"- Tiêu đề Slide 2: {request.Title}"); // Sửa: request.Title
            promptBuilder.AppendLine($"- Tóm tắt Slide 2: {request.Summary}"); // Sửa: request.Summary
            promptBuilder.AppendLine($"- Công thức Slide 2: {request.FormulaText}"); // Sửa: request.FormulaText
            promptBuilder.AppendLine($"- Giải thích Slide 2: {request.Explanation}"); // Sửa: request.Explanation
            promptBuilder.AppendLine($"- Ví dụ Slide 2: {request.ExampleText}"); // Sửa: request.ExampleText
            promptBuilder.AppendLine($"- Nguồn Slide 2: {request.Source}"); // Sửa: request.Source
            promptBuilder.AppendLine(jsonFormatPrompt);
            string finalPrompt = promptBuilder.ToString();

            var jsonResponse = await _geminiService.GenerateContentAsync(finalPrompt);
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                throw new InvalidOperationException("Gemini không trả về nội dung.");
            }

            _logger.LogInformation("--- Raw response from Gemini (before cleaning): {RawResponse}", jsonResponse);
            string cleanedJsonResponse = jsonResponse.Trim().Trim('"');
            if (cleanedJsonResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanedJsonResponse = cleanedJsonResponse.Substring(7);
                if (cleanedJsonResponse.EndsWith("```"))
                {
                    cleanedJsonResponse = cleanedJsonResponse.Substring(0, cleanedJsonResponse.Length - 3);
                }
            }
            cleanedJsonResponse = cleanedJsonResponse.Replace("\\\"", "\"");
            cleanedJsonResponse = cleanedJsonResponse.Trim();
            _logger.LogInformation("--- Cleaned JSON to be parsed: {CleanedResponse}", cleanedJsonResponse);

            try
            {
                var geminiContents = JsonSerializer.Deserialize<List<GeminiContentResponse>>(cleanedJsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (geminiContents == null || geminiContents.Count != 2)
                {
                    _logger.LogError("Gemini không trả về 2 mục JSON như yêu cầu. Response: {Response}", cleanedJsonResponse);
                    throw new InvalidOperationException("Gemini không trả về JSON có 2 mục như yêu cầu.");
                }

                var finalContentList = new List<Content>();
                foreach (var geminiContent in geminiContents)
                {
                    var newContent = new Content
                    {
                        TopicID = topic.TopicID,
                        Title = geminiContent.Title,
                        Summary = geminiContent.Summary,                        
                        Formulas = new List<Formula>(),
                        Examples = new List<Example>()
                    };

                    if (!string.IsNullOrWhiteSpace(geminiContent.Formula))
                    {
                        newContent.Formulas.Add(new Formula
                        {
                            FormulaText = geminiContent.Formula,
                            Explanation = geminiContent.Explanation
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(geminiContent.Example))
                    {
                        newContent.Examples.Add(new Example
                        {
                            ExampleText = geminiContent.Example
                        });
                    }
                    finalContentList.Add(newContent);
                }

                return finalContentList;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini JSON response. Cleaned response was: {JsonResponse}", cleanedJsonResponse);
                throw;
            }
        }

        private Task<string> GetGdptContextAsync(Topic topic)
        {
            _logger.LogWarning("GetGdptContextAsync không còn được sử dụng trong luồng này.");
            return Task.FromResult("");
        }

        public async Task<(MemoryStream stream, string fileName)> GenerateSlidesFromTopicAsync(int topicId, string templateName)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new FileNotFoundException($"Không tìm thấy Topic với ID: {topicId}");
            }
            _logger.LogInformation("Bắt đầu tạo slide cho Topic: {TopicName} (từ template JSON)", topic.Name);

            var tempRequest = new GenerationRequest
            {
                TopicId = topicId,
                TemplateName = templateName
            };
            var contentList = await GetOrGenerateContentAsync(topic, tempRequest);

            var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);
            string templateJson = await File.ReadAllTextAsync(templatePath);
            _logger.LogInformation("Bắt đầu tạo PPTX từ JSON template...");

            MemoryStream pptxStream = await _powerpointService.GeneratePptxFromJsonTemplateAsync(
                contentList,
                templateJson,
                topic.Name
            );

            string safeFileName = string.Concat((topic.Name ?? "Untitled").Split(Path.GetInvalidFileNameChars()));
            return (pptxStream, $"{safeFileName}.pptx");
        }

        public async Task<(MemoryStream stream, string fileName)> GenerateSlidesFromPptxTemplateAsync(GenerationRequest request)
        {
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                _logger.LogWarning("Không tìm thấy Topic ID {TopicId}. Sẽ dùng dữ liệu từ request.", request.TopicId);
                topic = new Topic
                {
                    TopicID = request.TopicId,
                    Name = request.Name, // Sửa: request.Name
                    Objectives = request.Objectives, // Sửa: request.Objectives
                    Source = request.Source, // Sửa: request.Source
                    Class = new Class { Name = "Không rõ" }
                };
            }
            _logger.LogInformation("Bắt đầu tạo slide cho Topic: {TopicName} (từ template .pptx)", topic.Name);

            var contentList = await GetOrGenerateContentAsync(topic, request);

            _logger.LogInformation("--- DEBUG: DỮ LIỆU TỪ GEMINI ---");
            _logger.LogInformation("Content for Slide 2 Title: {Title}", contentList[0]?.Title);
            _logger.LogInformation("Content for Slide 3 Title: {Title}", contentList[1]?.Title);

            var templatePptxPath = Path.Combine(_env.WebRootPath, "templates", request.TemplateName);
            if (!File.Exists(templatePptxPath))
            {
                throw new FileNotFoundException($"Không tìm thấy file PPTX template: {request.TemplateName}", templatePptxPath);
            }
            _logger.LogInformation("Bắt đầu tạo PPTX từ file template .pptx...");

            MemoryStream pptxStream = await _powerpointService.GeneratePptxFromPptxTemplateAsync(
                request,
                topic,
                contentList,
                templatePptxPath
            );

            string safeFileName = string.Concat((topic.Name ?? "Untitled").Split(Path.GetInvalidFileNameChars()));
            return (pptxStream, $"{safeFileName}.pptx");
        }
    }
}