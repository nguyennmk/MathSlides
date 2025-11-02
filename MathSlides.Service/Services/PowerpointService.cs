using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Hosting; // <-- ĐÃ THAY ĐỔI
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MathSlides.Service.Services
{
    public class PowerpointService : IPowerpointService
    {
        private readonly IPowerpointRepository _powerpointRepository;
        private readonly ILogger<PowerpointService> _logger;
        // Sử dụng IWebHostEnvironment để làm việc với wwwroot
        private readonly IWebHostEnvironment _webHostEnvironment; // <-- ĐÃ THAY ĐỔI

        public PowerpointService(
            IPowerpointRepository powerpointRepository,
            ILogger<PowerpointService> logger,
            IWebHostEnvironment webHostEnvironment) // <-- ĐÃ THAY ĐỔI
        {
            _powerpointRepository = powerpointRepository;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment; // <-- ĐÃ THAY ĐỔI
        }

        public async Task<PowerpointImportResponse> ImportPowerpointAsync(Stream fileStream, string fileName, string? name = null, string? description = null)
        {
            try
            {
                // Đọc file vào memory stream
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Chuyển đổi PowerPoint sang JSON
                var (jsonContent, slideCount) = await ConvertPowerpointToJsonAsync(memoryStream);

                // Tạo tên file JSON độc nhất để tránh ghi đè
                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
                var jsonFileName = Path.ChangeExtension(uniqueFileName, ".json");

                var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

                // Lấy đường dẫn VẬT LÝ đến thư mục wwwroot
                var webRootPath = _webHostEnvironment.WebRootPath;

                // Gọi Repository để LƯU FILE VẬT LÝ
                // (Chúng ta giả định SaveFileAsync trả về đường dẫn vật lý đầy đủ)
                var physicalPath = await _powerpointRepository.SaveFileAsync(jsonBytes, jsonFileName, webRootPath, "Templates");

                // TẠO ĐƯỜNG DẪN TƯƠNG ĐỐI (RELATIVE PATH)
                // Đây là đường dẫn mà React, CSDL và AI sẽ sử dụng
                // Nó bắt đầu bằng "/" và trỏ vào bên trong wwwroot
                var relativePath = $"/Templates/{jsonFileName}";

                return new PowerpointImportResponse
                {
                    // TRẢ VỀ ĐƯỜNG DẪN TƯƠNG ĐỐI
                    TemplatePath = relativePath, // <-- ĐÃ SỬA
                    FileName = jsonFileName,
                    JsonContent = jsonContent,
                    SlideCount = slideCount,
                    Message = $"PowerPoint file đã được chuyển đổi thành công. Tìm thấy {slideCount} slide(s)."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import PowerPoint file: {FileName}", fileName);
                throw new InvalidOperationException($"Không thể chuyển đổi PowerPoint file: {ex.Message}", ex);
            }
        }

        public async Task<string> SaveTemplatePathAsync(string templatePath, int templateId)
        {
            var template = await _powerpointRepository.UpdateTemplatePathAsync(templateId, templatePath);
            return template.TemplatePath;
        }

        public async Task<PowerpointImportResponse> GetPowerpointInfoAsync(string templatePath)
        {
            try
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var jsonContent = await _powerpointRepository.ReadFileAsync(templatePath, webRootPath);

                // Parse JSON để lấy thông tin
                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                var slideCount = root.TryGetProperty("SlideCount", out var slideCountProp) 
                    ? slideCountProp.GetInt32() 
                    : 0;

                // Lấy tên file từ templatePath
                var fileName = Path.GetFileName(templatePath);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.GetFileName(templatePath.TrimStart('/'));
                }

                return new PowerpointImportResponse
                {
                    TemplatePath = templatePath,
                    FileName = fileName,
                    JsonContent = jsonContent,
                    SlideCount = slideCount,
                    Message = $"Đã tải thông tin PowerPoint thành công. Tìm thấy {slideCount} slide(s)."
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File không tồn tại: {TemplatePath}", templatePath);
                throw new KeyNotFoundException($"File không tồn tại: {templatePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc file PowerPoint: {TemplatePath}", templatePath);
                throw new InvalidOperationException($"Không thể đọc file PowerPoint: {ex.Message}", ex);
            }
        }

        public async Task<PowerpointImportResponse> UpdatePowerpointInfoAsync(string templatePath, string jsonContent)
        {
            try
            {
                var webRootPath = _webHostEnvironment.WebRootPath;

                // Validate JSON content
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    var root = jsonDoc.RootElement;

                    var slideCount = root.TryGetProperty("SlideCount", out var slideCountProp)
                        ? slideCountProp.GetInt32()
                        : 0;

                    // Update file
                    await _powerpointRepository.UpdateFileAsync(templatePath, jsonContent, webRootPath);

                    // Lấy tên file từ templatePath
                    var fileName = Path.GetFileName(templatePath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = Path.GetFileName(templatePath.TrimStart('/'));
                    }

                    return new PowerpointImportResponse
                    {
                        TemplatePath = templatePath,
                        FileName = fileName,
                        JsonContent = jsonContent,
                        SlideCount = slideCount,
                        Message = $"Đã cập nhật file PowerPoint thành công. Tìm thấy {slideCount} slide(s)."
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "JSON content không hợp lệ: {TemplatePath}", templatePath);
                    throw new ArgumentException($"JSON content không hợp lệ: {ex.Message}", ex);
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File không tồn tại: {TemplatePath}", templatePath);
                throw new KeyNotFoundException($"File không tồn tại: {templatePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật file PowerPoint: {TemplatePath}", templatePath);
                throw new InvalidOperationException($"Không thể cập nhật file PowerPoint: {ex.Message}", ex);
            }
        }

        //
        // --- CÁC HÀM PRIVATE GIỮ NGUYÊN ---
        //

        private async Task<(string jsonContent, int slideCount)> ConvertPowerpointToJsonAsync(Stream fileStream)
        {
            return await Task.Run(() =>
            {
                using var presentationDocument = PresentationDocument.Open(fileStream, false);
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart == null)
                {
                    throw new InvalidOperationException("File PowerPoint không hợp lệ: không tìm thấy presentation part");
                }

                var presentation = presentationPart.Presentation;
                if (presentation?.SlideIdList == null)
                {
                    throw new InvalidOperationException("File PowerPoint không hợp lệ: không tìm thấy slides");
                }

                var slides = new List<SlideData>();
                var slideCount = 0;

                foreach (SlideId slideId in presentation.SlideIdList.Elements<SlideId>())
                {
                    slideCount++;
                    var slideData = ExtractSlideData(presentationPart, slideId);
                    if (slideData != null)
                    {
                        slides.Add(slideData);
                    }
                }

                var result = new
                {
                    SlideCount = slideCount,
                    Slides = slides,
                    CreatedAt = DateTime.UtcNow
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var jsonContent = JsonSerializer.Serialize(result, options);
                return (jsonContent, slideCount);
            });
        }

        private SlideData? ExtractSlideData(PresentationPart presentationPart, SlideId slideId)
        {
            try
            {
                var relationshipId = slideId.RelationshipId?.Value;
                if (string.IsNullOrEmpty(relationshipId))
                {
                    return null;
                }

                var slidePart = (SlidePart?)presentationPart.GetPartById(relationshipId);
                if (slidePart?.Slide == null)
                {
                    return null;
                }

                var slide = slidePart.Slide;
                var slideData = new SlideData
                {
                    SlideId = slideId.RelationshipId?.Value ?? "",
                    Shapes = new List<ShapeData>()
                };

                // Lấy CommonSlideData
                if (slide.CommonSlideData?.ShapeTree != null)
                {
                    foreach (var shape in slide.CommonSlideData.ShapeTree.Elements<DocumentFormat.OpenXml.Presentation.Shape>())
                    {
                        var shapeData = ExtractShapeData(shape);
                        if (shapeData != null)
                        {
                            slideData.Shapes.Add(shapeData);
                        }
                    }

                    // Lấy text từ TextBody
                    foreach (var textBody in slide.CommonSlideData.ShapeTree.Descendants<DocumentFormat.OpenXml.Drawing.TextBody>())
                    {
                        var text = ExtractTextFromTextBody(textBody);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            slideData.Texts.Add(text);
                        }
                    }
                }

                return slideData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể extract slide data cho slide ID: {SlideId}", slideId.RelationshipId);
                return null;
            }
        }

        private ShapeData? ExtractShapeData(DocumentFormat.OpenXml.Presentation.Shape shape)
        {
            try
            {
                var shapeData = new ShapeData
                {
                    ShapeId = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value ?? 0,
                    Name = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value ?? ""
                };

                // Lấy text từ shape
                var textBody = shape.GetFirstChild<DocumentFormat.OpenXml.Drawing.TextBody>();
                if (textBody != null)
                {
                    shapeData.Text = ExtractTextFromTextBody(textBody);
                }

                return shapeData;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractTextFromTextBody(DocumentFormat.OpenXml.Drawing.TextBody textBody)
        {
            var textBuilder = new StringBuilder();
            foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
            {
                foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
                {
                    var text = run.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                    if (text?.Text != null)
                    {
                        textBuilder.Append(text.Text);
                    }
                }
                textBuilder.AppendLine();
            }
            return textBuilder.ToString().Trim();
        }


        private class SlideData
        {
            public string SlideId { get; set; } = string.Empty;
            public List<ShapeData> Shapes { get; set; } = new();
            public List<string> Texts { get; set; } = new();
        }

        private class ShapeData
        {
            public uint ShapeId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }
    }
}