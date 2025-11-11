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

namespace MathSlides.Service.Services
{
    public class SlideGenerationService : ISlideGenerationService
    {
        private readonly IGeminiService _geminiService;

        private readonly IPowerpointService _powerpointService;

        private readonly ITopicRepository _topicRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger<SlideGenerationService> _logger;

        private readonly IWebHostEnvironment _env;
        private class GeminiContentResponse { [JsonPropertyName("title")] public string Title { get; set; } = string.Empty; [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty; [JsonPropertyName("formulas")] public List<GeminiFormula> Formulas { get; set; } = new List<GeminiFormula>(); [JsonPropertyName("examples")] public List<GeminiExample> Examples { get; set; } = new List<GeminiExample>(); }
        private class GeminiFormula { [JsonPropertyName("formulaText")] public string FormulaText { get; set; } = string.Empty; [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty; }
        private class GeminiExample { [JsonPropertyName("exampleText")] public string ExampleText { get; set; } = string.Empty; }

        public SlideGenerationService(
            IGeminiService geminiService,
            IPowerpointService powerpointService,
            ITopicRepository topicRepository,
            IContentRepository contentRepository,
            ILogger<SlideGenerationService> logger,
            IWebHostEnvironment env) 
        {
            _geminiService = geminiService;
            _powerpointService = powerpointService;
            _topicRepository = topicRepository;
            _contentRepository = contentRepository;
            _logger = logger;
            _env = env;
        }

        public async Task<(MemoryStream stream, string fileName)> GenerateSlidesFromTopicAsync(int topicId, string templateName)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new FileNotFoundException($"Không tìm thấy Topic với ID: {topicId}");
            }
            _logger.LogInformation("Bắt đầu tạo slide cho Topic: {TopicName}", topic.Name);

            var contentList = await _contentRepository.GetContentsByTopicIdAsync(topicId);
            if (contentList == null || !contentList.Any())
            {
                _logger.LogInformation("Không tìm thấy nội dung có sẵn. Bắt đầu gọi Gemini...");
                string prompt = $"Hãy đóng vai trò là một chuyên gia biên soạn sách giáo khoa Toán học Việt Nam cho chương trình GDPT 2018." +
                                $"Tạo nội dung bài giảng cho chủ đề: '{topic.Name}' (Lớp {topic.Class.Name}, Cấp {topic.Class.Grade.Name}). " +
                                $"Hãy trả về một chuỗi JSON duy nhất (một dòng, không ngắt dòng) là một MẢNG (array) các nội dung." +
                                $"Mỗi nội dung (content) phải có các khóa: " +
                                $"1. 'title': (string) Tiêu đề của slide (ví dụ: 'Tóm tắt lý thuyết', 'Công thức chính', 'Ví dụ 1')." +
                                $"2. 'summary': (string) Nội dung văn bản chính cho slide đó (dùng mã LaTeX)." +
                                $"3. 'formulas': (array of objects) Mảng các công thức. Mỗi object có 'formulaText' và 'explanation'." +
                                $"4. 'examples': (array of objects) Mảng các ví dụ. Mỗi object có 'exampleText'." +
                                $"Ví dụ JSON: [ {{\"title\":\"Tóm tắt\", \"summary\":\"...\", \"formulas\":[], \"examples\":[]}}, {{\"title\":\"Công thức\", \"summary\":\"\", \"formulas\":[...], \"examples\":[]}} ]";

                var jsonResponse = await _geminiService.GenerateContentAsync(prompt);
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    throw new InvalidOperationException("Gemini không trả về nội dung.");
                }

                try
                {
                    var geminiContents = JsonSerializer.Deserialize<List<GeminiContentResponse>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (geminiContents == null || !geminiContents.Any())
                    {
                        throw new InvalidOperationException("Gemini trả về JSON rỗng hoặc không hợp lệ.");
                    }

                    contentList = new List<Content>();
                    foreach (var geminiContent in geminiContents)
                    {
                        var newContent = new Content
                        {
                            TopicID = topicId,
                            Title = geminiContent.Title,
                            Summary = geminiContent.Summary,
                            Formulas = geminiContent.Formulas
                                         .Select(f => new Formula { FormulaText = f.FormulaText, Explanation = f.Explanation })
                                         .ToList(),
                            Examples = geminiContent.Examples
                                         .Select(e => new Example { ExampleText = e.ExampleText })
                                         .ToList()
                        };
                        contentList.Add(newContent);
                    }

                    await _contentRepository.CreateBulkContentAsync(contentList);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Gemini JSON response. Response was: {JsonResponse}", jsonResponse);
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Đã tìm thấy nội dung có sẵn trong DB. Bỏ qua bước gọi Gemini.");
            }

            var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file JSON template: {templateName}", templatePath);
            }
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
    }
}