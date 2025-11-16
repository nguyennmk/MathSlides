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
        private readonly IGDPTService _gdptService;

        // Các DTO nội bộ cho Gemini
        private class GeminiContentResponse
        {
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
            [JsonPropertyName("formulas")] public List<GeminiFormula> Formulas { get; set; } = new List<GeminiFormula>();
            [JsonPropertyName("examples")] public List<GeminiExample> Examples { get; set; } = new List<GeminiExample>();
        }
        private class GeminiFormula
        {
            [JsonPropertyName("formulaText")] public string FormulaText { get; set; } = string.Empty;
            [JsonPropertyName("explanation")] public string Explanation { get; set; } = string.Empty;
        }
        private class GeminiExample
        {
            [JsonPropertyName("exampleText")] public string ExampleText { get; set; } = string.Empty;
        }

        // Constructor
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

        // --- HÀM HELPER: LẤY HOẶC TẠO NỘI DUNG (ĐÃ NÂNG CẤP) ---
        private async Task<List<Content>> GetOrGenerateContentAsync(Topic topic)
        {
            var contentList = await _contentRepository.GetContentsByTopicIdAsync(topic.TopicID);
            if (contentList != null && contentList.Any())
            {
                _logger.LogInformation("Đã tìm thấy nội dung có sẵn trong DB. Bỏ qua bước gọi Gemini.");
                return contentList;
            }

            _logger.LogInformation("Không tìm thấy nội dung có sẵn. Bắt đầu gọi Gemini...");

            // 1. Lấy prompt yêu cầu format JSON (như cũ)
            string jsonFormatPrompt = $"Hãy trả về một chuỗi JSON duy nhất (một dòng, không ngắt dòng) là một MẢNG (array) các nội dung." +
                                      $"Mỗi nội dung (content) phải có các khóa: " +
                                      $"1. 'title': (string) Tiêu đề của slide." +
                                      $"2. 'summary': (string) Nội dung văn bản chính cho slide đó (dùng mã LaTeX)." +
                                      $"3. 'formulas': (array of objects) Mảng các công thức. Mỗi object có 'formulaText' và 'explanation'." +
                                      $"4. 'examples': (array of objects) Mảng các ví dụ. Mỗi object có 'exampleText'.";

            // 2. LẤY NGỮ CẢNH TỪ GDPT (LOGIC NÂNG CẤP)
            _logger.LogInformation("Đang tải dữ liệu GDPT/Curriculum có sẵn để làm ngữ cảnh...");
            string contextInstruction = "";
            try
            {
                var curriculumList = await _gdptService.GetCurriculumByGradeAndClassAsync(topic.Class.Grade.Name, topic.Class.Name);
                var curriculum = curriculumList?.FirstOrDefault(t => t.TopicID == topic.TopicID);

                if (curriculum != null)
                {
                    _logger.LogInformation("Đã tìm thấy dữ liệu GDPT. Sử dụng làm ngữ cảnh cho Gemini.");
                    var contextData = new
                    {
                        topicName = curriculum.TopicName,
                        strandName = curriculum.StrandName,
                        objectives = curriculum.Objectives,
                        existingContents = curriculum.Contents.Select(c => new
                        {
                            title = c.Title,
                            summary = c.Summary,
                            formulas = c.Formulas.Select(f => f.FormulaText)
                        })
                    };
                    string contextJson = JsonSerializer.Serialize(contextData);

                    contextInstruction = $"Dưới đây là dữ liệu giáo trình (GDPT) hiện có về chủ đề này. Hãy sử dụng thông tin này làm NGUỒN NGỮ CẢNH CHÍNH để phát triển, bổ sung và hoàn thiện nội dung. Đảm bảo nội dung bạn tạo ra đầy đủ, chi tiết và bám sát mục tiêu (objectives) đã cho:\n\n{contextJson}\n\n";
                }
                else
                {
                    _logger.LogInformation("Không tìm thấy dữ liệu GDPT. Gemini sẽ tự tạo từ đầu.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải dữ liệu GDPT. Gemini sẽ tự tạo từ đầu.");
            }

            // 3. Tạo prompt cuối cùng
            string finalPrompt = $"Hãy đóng vai trò là một chuyên gia biên soạn sách giáo khoa Toán học Việt Nam cho chương trình GDPT 2018." +
                                 $"Tạo nội dung bài giảng chi tiết cho chủ đề: '{topic.Name}' (Lớp {topic.Class.Name}, Cấp {topic.Class.Grade.Name}). " +
                                 contextInstruction + // Ngữ cảnh GDPT (nếu có)
                                 jsonFormatPrompt; // Yêu cầu format output

            // --- HẾT LOGIC MỚI ---

            var jsonResponse = await _geminiService.GenerateContentAsync(finalPrompt);
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
                        TopicID = topic.TopicID,
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
                return contentList;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini JSON response. Response was: {JsonResponse}", jsonResponse);
                throw;
            }
        }


        // --- LUỒNG 1 (JSON TEMPLATE) ---
        public async Task<(MemoryStream stream, string fileName)> GenerateSlidesFromTopicAsync(int topicId, string templateName)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new FileNotFoundException($"Không tìm thấy Topic với ID: {topicId}");
            }
            _logger.LogInformation("Bắt đầu tạo slide cho Topic: {TopicName} (từ template JSON)", topic.Name);

            // 1. Lấy hoặc tạo nội dung (đã bao gồm logic Gemini nâng cấp)
            var contentList = await GetOrGenerateContentAsync(topic);

            // 2. Tải template JSON
            var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file JSON template: {templateName}", templatePath);
            }
            string templateJson = await File.ReadAllTextAsync(templatePath);
            _logger.LogInformation("Bắt đầu tạo PPTX từ JSON template...");

            // 3. Gọi PowerpointService (luồng JSON)
            MemoryStream pptxStream = await _powerpointService.GeneratePptxFromJsonTemplateAsync(
                contentList,
                templateJson,
                topic.Name
            );

            string safeFileName = string.Concat((topic.Name ?? "Untitled").Split(Path.GetInvalidFileNameChars()));
            return (pptxStream, $"{safeFileName}.pptx");
        }


        // --- LUỒNG 2 (PPTX TEMPLATE) ---
        public async Task<(MemoryStream stream, string fileName)> GenerateSlidesFromPptxTemplateAsync(int topicId, string templatePptxName)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new FileNotFoundException($"Không tìm thấy Topic với ID: {topicId}");
            }
            _logger.LogInformation("Bắt đầu tạo slide cho Topic: {TopicName} (từ template .pptx)", topic.Name);

            // 1. Lấy hoặc tạo nội dung (DÙNG LẠI HÀM HELPER)
            var contentList = await GetOrGenerateContentAsync(topic);

            // *** THÊM 3 DÒNG CODE NÀY VÀO ***
            _logger.LogInformation("--- DEBUG: DỮ LIỆU TỪ DATABASE ---");
            _logger.LogInformation("Topic Name: {Name}", topic.Name);
            _logger.LogInformation("First Content Title: {Title}", contentList.FirstOrDefault()?.Title);
            // *** KẾT THÚC THÊM CODE ***

            // 2. Tải template .PPTX
            var templatePptxPath = Path.Combine(_env.WebRootPath, "templates", templatePptxName);
            if (!File.Exists(templatePptxPath))
            {
                throw new FileNotFoundException($"Không tìm thấy file PPTX template: {templatePptxName}", templatePptxPath);
            }
            _logger.LogInformation("Bắt đầu tạo PPTX từ file template .pptx...");

            // 3. Gọi PowerpointService (luồng PPTX mới)
            MemoryStream pptxStream = await _powerpointService.GeneratePptxFromPptxTemplateAsync(
                topic, // Truyền cả đối tượng topic
                contentList,
                templatePptxPath
            );

            string safeFileName = string.Concat((topic.Name ?? "Untitled").Split(Path.GetInvalidFileNameChars()));
            return (pptxStream, $"{safeFileName}.pptx");
        }
    }
}