using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace MathSlides.Service.Services
{
    public class PowerpointService : IPowerpointService
    {
        private readonly IPowerpointRepository _powerpointRepository;
        private readonly ILogger<PowerpointService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PowerpointService(
            IPowerpointRepository powerpointRepository,
            ILogger<PowerpointService> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _powerpointRepository = powerpointRepository;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- CÁC HÀM GỐC CỦA BẠN (IMPORT, GET INFO, ETC.) ---
        #region Original Import/Management Functions

        public async Task<PowerpointImportResponse> ImportPowerpointAsync(Stream fileStream, string fileName, string? name = null, string? description = null)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var (jsonContent, slideCount) = await ConvertPowerpointToJsonAsync(memoryStream);

                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
                var jsonFileName = Path.ChangeExtension(uniqueFileName, ".json");
                var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
                var webRootPath = _webHostEnvironment.WebRootPath;
                var physicalPath = await _powerpointRepository.SaveFileAsync(jsonBytes, jsonFileName, webRootPath, "Templates");
                var relativePath = $"/Templates/{jsonFileName}";

                return new PowerpointImportResponse
                {
                    TemplatePath = relativePath,
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

                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                var slideCount = root.TryGetProperty("SlideCount", out var slideCountProp)
                    ? slideCountProp.GetInt32()
                    : 0;

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
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    var root = jsonDoc.RootElement;

                    var slideCount = root.TryGetProperty("SlideCount", out var slideCountProp)
                        ? slideCountProp.GetInt32()
                        : 0;

                    await _powerpointRepository.UpdateFileAsync(templatePath, jsonContent, webRootPath);

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
                    Shapes = new List<ShapeData>(),
                    Texts = new List<string>() // Khởi tạo list này
                };

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

        #endregion

        // --- LUỒNG 1: TẠO TỪ JSON (GIỮ NGUYÊN) ---
        #region JSON Template Generation

        private class PptxTemplate { public long SlideWidthEmu { get; set; } public long SlideHeightEmu { get; set; } public long MarginEmu { get; set; } public Layouts Layouts { get; set; } = new(); }
        private class Layouts { public Layout TitleSlide { get; set; } = new(); public Layout ContentSlide { get; set; } = new(); public SplitLayout SplitSlide { get; set; } = new(); }
        private class Layout { public ShapeProps Title { get; set; } = new(); public ShapeProps Content { get; set; } = new(); public ShapeProps Subtitle { get; set; } = new(); }
        private class SplitLayout { public ShapeProps Title { get; set; } = new(); public ShapeProps LeftContent { get; set; } = new(); public ShapeProps RightContent { get; set; } = new(); }
        private class ShapeProps { public long X { get; set; } public long Y { get; set; } public long W { get; set; } public long H { get; set; } public int FontSize { get; set; } public string Align { get; set; } = "left"; }

        public async Task<MemoryStream> GeneratePptxFromJsonTemplateAsync(List<Content> contentList, string templateJson, string topicName)
        {
            var template = JsonSerializer.Deserialize<PptxTemplate>(templateJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (template == null)
            {
                throw new InvalidOperationException("Không thể đọc file JSON template.");
            }

            MemoryStream ms = new MemoryStream();
            await Task.Run(() =>
            {
                using (PresentationDocument pptDoc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation, true))
                {
                    PresentationPart presentationPart = pptDoc.AddPresentationPart();
                    presentationPart.Presentation = new P.Presentation();
                    CreatePresentationParts(presentationPart);

                    uint shapeIdCounter = 1;

                    var titleLayout = template.Layouts.TitleSlide;
                    SlidePart slidePart1 = CreateSlidePart(presentationPart);
                    ShapeTree shapeTree1 = slidePart1.Slide.CommonSlideData.ShapeTree;

                    shapeTree1.Append(CreateTextShape(
                        (shapeIdCounter++).ToString(),
                        topicName,
                        titleLayout.Title.X, titleLayout.Title.Y, titleLayout.Title.W, titleLayout.Title.H,
                        titleLayout.Title.FontSize, GetAlignment(titleLayout.Title.Align)
                    ));
                    var firstContent = contentList.FirstOrDefault();
                    if (firstContent != null)
                    {
                        shapeTree1.Append(CreateTextShape(
                            (shapeIdCounter++).ToString(),
                            firstContent.Title,
                            titleLayout.Subtitle.X, titleLayout.Subtitle.Y, titleLayout.Subtitle.W, titleLayout.Subtitle.H,
                            titleLayout.Subtitle.FontSize, GetAlignment(titleLayout.Subtitle.Align)
                        ));
                    }

                    foreach (var content in contentList)
                    {
                        if (!string.IsNullOrWhiteSpace(content.Summary))
                        {
                            var contentLayout = template.Layouts.ContentSlide;
                            SlidePart slidePartN = CreateSlidePart(presentationPart);
                            ShapeTree shapeTreeN = slidePartN.Slide.CommonSlideData.ShapeTree;

                            shapeTreeN.Append(CreateTextShape(
                                (shapeIdCounter++).ToString(), content.Title,
                                contentLayout.Title.X, contentLayout.Title.Y, contentLayout.Title.W, contentLayout.Title.H,
                                contentLayout.Title.FontSize, GetAlignment(contentLayout.Title.Align)
                            ));
                            shapeTreeN.Append(CreateTextShape(
                                (shapeIdCounter++).ToString(), content.Summary,
                                contentLayout.Content.X, contentLayout.Content.Y, contentLayout.Content.W, contentLayout.Content.H,
                                contentLayout.Content.FontSize, GetAlignment(contentLayout.Content.Align)
                            ));
                        }
                        if (content.Formulas.Any() || content.Examples.Any())
                        {
                            var splitLayout = template.Layouts.SplitSlide;
                            SlidePart slidePartSplit = CreateSlidePart(presentationPart);
                            ShapeTree shapeTreeSplit = slidePartSplit.Slide.CommonSlideData.ShapeTree;

                            shapeTreeSplit.Append(CreateTextShape(
                                (shapeIdCounter++).ToString(), content.Title,
                                splitLayout.Title.X, splitLayout.Title.Y, splitLayout.Title.W, splitLayout.Title.H,
                                splitLayout.Title.FontSize, GetAlignment(splitLayout.Title.Align)
                            ));

                            string formulaText = string.Join("\n\n", content.Formulas.Select(f => f.FormulaText + (f.Explanation != null ? $"\n({f.Explanation})" : "")));
                            shapeTreeSplit.Append(CreateTextShape(
                                (shapeIdCounter++).ToString(), formulaText,
                                splitLayout.LeftContent.X, splitLayout.LeftContent.Y, splitLayout.LeftContent.W, splitLayout.LeftContent.H,
                                splitLayout.LeftContent.FontSize, GetAlignment(splitLayout.LeftContent.Align)
                            ));

                            string exampleText = string.Join("\n\n", content.Examples.Select(e => e.ExampleText));
                            shapeTreeSplit.Append(CreateTextShape(
                                (shapeIdCounter++).ToString(), exampleText,
                                splitLayout.RightContent.X, splitLayout.RightContent.Y, splitLayout.RightContent.W, splitLayout.RightContent.H,
                                splitLayout.RightContent.FontSize, GetAlignment(splitLayout.RightContent.Align)
                            ));
                        }
                    }
                    presentationPart.Presentation.Save();
                }
            });

            ms.Position = 0;
            return ms;
        }

        private D.TextAlignmentTypeValues GetAlignment(string align)
        {
            return align?.ToLower() switch
            {
                "center" => D.TextAlignmentTypeValues.Center,
                "right" => D.TextAlignmentTypeValues.Right,
                _ => D.TextAlignmentTypeValues.Left,
            };
        }

        private P.Shape CreateTextShape(string id, string text, long x, long y, long w, long h, int fontSize, D.TextAlignmentTypeValues alignment)
        {
            P.Shape shape = new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties() { Id = (UInt32Value)uint.Parse(id), Name = $"TextBox {id}" },
                    new P.NonVisualShapeDrawingProperties(new D.ShapeLocks() { NoGrouping = true }),
                    new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape())),
                new P.ShapeProperties(
                    new D.Transform2D(
                        new D.Offset() { X = x, Y = y },
                        new D.Extents() { Cx = w, Cy = h }),
                    new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
                new P.TextBody(
                    new D.BodyProperties(),
                    new D.ListStyle(),
                    new D.Paragraph(
                        new D.ParagraphProperties(new D.DefaultRunProperties()) { Alignment = alignment },
                        new D.Run(
                            new D.RunProperties() { Language = "vi-VN", FontSize = fontSize * 100, Dirty = false },
                            new D.Text(text ?? "")
                        )
                    )
                )
            );
            return shape;
        }

        private SlidePart CreateSlidePart(PresentationPart presentationPart)
        {
            SlideLayoutPart slideLayoutPart = presentationPart.SlideMasterParts.First().SlideLayoutParts.First();
            SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new P.Slide(new P.CommonSlideData(new P.ShapeTree()), new P.ColorMapOverride(new D.MasterColorMapping()));
            slidePart.AddPart(slideLayoutPart);

            if (presentationPart.Presentation.SlideIdList == null)
                presentationPart.Presentation.SlideIdList = new P.SlideIdList();

            uint slideIndex = (uint)presentationPart.Presentation.SlideIdList.Count() + 256U;
            string relId = presentationPart.GetIdOfPart(slidePart);
            presentationPart.Presentation.SlideIdList.AppendChild(new P.SlideId() { Id = slideIndex, RelationshipId = relId });

            return slidePart;
        }

        private void CreatePresentationParts(PresentationPart presentationPart)
        {
            SlideMasterPart slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
            slideMasterPart.SlideMaster = new P.SlideMaster(
                new P.CommonSlideData(new P.ShapeTree()),
                new P.ColorMap(new D.MasterColorMapping(), new D.OverrideColorMapping()),
                new P.SlideLayoutIdList(new P.SlideLayoutId() { Id = 2147483649U, RelationshipId = "rId1" })
            );

            SlideLayoutPart slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
            slideLayoutPart.SlideLayout = new P.SlideLayout(
                new P.CommonSlideData(new P.ShapeTree()),
                new P.ColorMapOverride(new D.MasterColorMapping())
            );

            ThemePart themePart = presentationPart.AddNewPart<ThemePart>();
            themePart.Theme = new D.Theme() { Name = "Office Theme" };
            slideMasterPart.AddPart(themePart);
            presentationPart.AddPart(slideMasterPart);
        }

        #endregion


        // --- LUỒNG 2: TẠO TỪ TEMPLATE PPTX (PHƯƠNG PHÁP TAGGING) ---
        #region PPTX Template Generation (Tagging Method)

        public async Task<MemoryStream> GeneratePptxFromPptxTemplateAsync(Topic topic, List<Content> contentList, string templatePptxPath)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            if (!File.Exists(templatePptxPath))
            {
                throw new FileNotFoundException("Không tìm thấy file template .pptx", templatePptxPath);
            }

            // 1. Sao chép template vào MemoryStream
            var ms = new MemoryStream();
            using (var fs = new FileStream(templatePptxPath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(ms);
            }

            // 2. Mở file .pptx từ MemoryStream (true = cho phép ghi)
            using (PresentationDocument pptDoc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = pptDoc.PresentationPart;
                if (presentationPart?.Presentation?.SlideIdList == null)
                {
                    throw new InvalidOperationException("File template .pptx không hợp lệ.");
                }

                // 3. TẠO BẢN ĐỒ THAY THẾ (THEO "TAG" BẠN ĐẶT)
                var globalReplacements = new Dictionary<string, string>
                {
                    { "tpl_GradeName", topic.Class?.Grade?.Name ?? "" },
                    { "tpl_StrandName", topic.Strand?.Name ?? "" }
                };

                // 4. Xử lý Slide Tiêu đề (Slide 1)
                var slideParts = presentationPart.SlideParts.ToList();
                var titleSlidePart = slideParts.FirstOrDefault();
                var firstContent = contentList.FirstOrDefault();

                if (titleSlidePart != null)
                {
                    var titleSlideReplacements = new Dictionary<string, string>(globalReplacements)
                    {
                        { "tpl_TopicName", topic.Name },
                        { "tpl_ClassName", topic.Class?.Name ?? "" },
                        { "tpl_Objectives", topic.Objectives ?? "" },
                        { "tpl_Source", topic.Source ?? "" },
                        { "tpl_FirstContentTitle", firstContent?.Title ?? "" }
                    };
                    _logger.LogInformation("--- Đang xử lý Slide 1 (Tiêu đề) ---");
                    ReplacePlaceholdersByTag(titleSlidePart, titleSlideReplacements);
                }

                // 5. Xác định các Slide Mẫu (Vẫn theo quy ước Slide 2, 3)
                SlidePart? contentTemplateSlide = slideParts.Count > 1 ? slideParts[1] : null;
                SlidePart? splitTemplateSlide = slideParts.Count > 2 ? slideParts[2] : null;

                if (contentTemplateSlide == null || splitTemplateSlide == null)
                {
                    _logger.LogWarning("Template .pptx không đủ 3 slide mẫu. Sẽ chỉ điền slide 1.");
                }

                var slidesToInsert = new List<SlidePart>();
                uint uniqueSlideId = (uint)presentationPart.Presentation.SlideIdList.Count() + 256;

                // 6. GIỚI HẠN SLIDE (THEO YÊU CẦU MỚI)
                int slideCounter = 1; // Bắt đầu từ 1 vì đã có slide tiêu đề
                const int MAX_SLIDES = 7;

                // Lặp qua danh sách content (Bỏ qua content đầu tiên đã dùng cho slide 1)
                foreach (var content in contentList.Skip(1))
                {
                    if (slideCounter >= MAX_SLIDES)
                    {
                        _logger.LogInformation("Đã đạt giới hạn 7 slide. Dừng tạo thêm slide.");
                        break;
                    }

                    // Tạo slide Tóm tắt (Summary)
                    if (!string.IsNullOrWhiteSpace(content.Summary) && contentTemplateSlide != null && slideCounter < MAX_SLIDES)
                    {
                        var newContentSlide = CloneSlidePart(presentationPart, contentTemplateSlide, ref uniqueSlideId);
                        var contentReplacements = new Dictionary<string, string>(globalReplacements)
                        {
                            { "tpl_ContentTitle", content.Title },
                            { "tpl_ContentSummary", content.Summary }
                        };
                        _logger.LogInformation("--- Đang xử lý Slide {SlideNum} (Content: {Title}) ---", slideCounter + 1, content.Title);
                        ReplacePlaceholdersByTag(newContentSlide, contentReplacements);
                        slidesToInsert.Add(newContentSlide);
                        slideCounter++;
                    }

                    if (slideCounter >= MAX_SLIDES) break;

                    // Tạo slide Công thức/Ví dụ (Split)
                    if ((content.Formulas.Any() || content.Examples.Any() || content.Media.Any()) && splitTemplateSlide != null && slideCounter < MAX_SLIDES)
                    {
                        var newSplitSlide = CloneSlidePart(presentationPart, splitTemplateSlide, ref uniqueSlideId);

                        string formulaText = string.Join("\n\n", content.Formulas.Select(f => f.FormulaText));
                        string explanationText = string.Join("\n\n", content.Formulas.Where(f => !string.IsNullOrEmpty(f.Explanation)).Select(f => f.Explanation));
                        string exampleText = string.Join("\n\n", content.Examples.Select(e => e.ExampleText));
                        string mediaText = string.Join("\n", content.Media.Select(m => $"{m.Type}: {m.Url}"));

                        var splitReplacements = new Dictionary<string, string>(globalReplacements)
                        {
                            { "tpl_ContentTitle", content.Title },
                            { "tpl_ContentFormulas", string.IsNullOrWhiteSpace(formulaText) ? "Không có công thức." : formulaText },
                            { "tpl_ContentFormulaExplanations", string.IsNullOrWhiteSpace(explanationText) ? "" : explanationText },
                            { "tpl_ContentExamples", string.IsNullOrWhiteSpace(exampleText) ? "Không có ví dụ." : exampleText },
                            { "tpl_ContentMedia", string.IsNullOrWhiteSpace(mediaText) ? "Không có media." : mediaText }
                        };

                        _logger.LogInformation("--- Đang xử lý Slide {SlideNum} (Split: {Title}) ---", slideCounter + 1, content.Title);
                        ReplacePlaceholdersByTag(newSplitSlide, splitReplacements);
                        slidesToInsert.Add(newSplitSlide);
                        slideCounter++;
                    }
                }

                // 7. Thêm các slide đã tạo vào bài thuyết trình
                var slideIdList = presentationPart.Presentation.SlideIdList;
                foreach (var slidePart in slidesToInsert)
                {
                    slideIdList.Append(new P.SlideId()
                    {
                        Id = uniqueSlideId++,
                        RelationshipId = presentationPart.GetIdOfPart(slidePart)
                    });
                }

                // 8. Xóa các slide mẫu (Slide 2 và 3), nếu chúng tồn tại
                if (contentTemplateSlide != null)
                {
                    var contentTemplateRelId = presentationPart.GetIdOfPart(contentTemplateSlide);
                    var slideId = slideIdList.ChildElements.OfType<P.SlideId>().FirstOrDefault(s => s.RelationshipId == contentTemplateRelId);
                    if (slideId != null) slideIdList.RemoveChild(slideId);
                    presentationPart.DeletePart(contentTemplateSlide);
                }
                if (splitTemplateSlide != null)
                {
                    var splitTemplateRelId = presentationPart.GetIdOfPart(splitTemplateSlide);
                    var slideId = slideIdList.ChildElements.OfType<P.SlideId>().FirstOrDefault(s => s.RelationshipId == splitTemplateRelId);
                    if (slideId != null) slideIdList.RemoveChild(slideId);
                    presentationPart.DeletePart(splitTemplateSlide);
                }

                // 9. Xóa tất cả các slide còn lại (nếu có) VƯỢT QUÁ 7 slide
                while (slideIdList.Count() > MAX_SLIDES)
                {
                    var slideToRemove = slideIdList.ChildElements.OfType<P.SlideId>().LastOrDefault();
                    if (slideToRemove != null)
                    {
                        var partToRemove = presentationPart.GetPartById(slideToRemove.RelationshipId);
                        slideToRemove.Remove();
                        if (partToRemove != null)
                        {
                            presentationPart.DeletePart(partToRemove);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // 10. Lưu
                presentationPart.Presentation.Save();
            }

            ms.Position = 0;
            return ms;
        }

        // --- HÀM HELPER NÂNG CẤP (PHƯƠNG PHÁP TAGGING) ---
        // *** PHIÊN BẢN 5: SỬA LỖI GROUP SHAPE ***
        private void ReplacePlaceholdersByTag(SlidePart slidePart, Dictionary<string, string> replacements)
        {
            if (slidePart?.Slide == null) return;

            _logger.LogInformation("Đang quét các tag: {Tags}", string.Join(", ", replacements.Keys));

            // 1. Tìm TẤT CẢ các element có thuộc tính Name (tag)
            var namedElements = slidePart.Slide.Descendants<P.NonVisualDrawingProperties>()
                .Where(nvdp => nvdp.Name != null && replacements.ContainsKey(nvdp.Name.Value))
                .ToList();

            _logger.LogInformation("Tìm thấy {Count} tag khớp.", namedElements.Count);

            foreach (var nvdp in namedElements)
            {
                string tag = nvdp.Name.Value;
                string newText = replacements[tag];

                _logger.LogInformation("Đang xử lý tag: '{Tag}'", tag);

                // 2. *** SỬA LỖI GROUP SHAPE ***
                // Đi ngược (parent) lên để tìm container (P.Shape, P.GraphicFrame)
                // bất kể nó có bị group hay không
                OpenXmlCompositeElement container = null;
                var parent = nvdp.Parent;
                while (parent != null)
                {
                    // Nếu cha là Shape hoặc GraphicFrame, chúng ta tìm thấy nó!
                    if (parent is P.Shape || parent is P.GraphicFrame)
                    {
                        container = parent as OpenXmlCompositeElement;
                        _logger.LogInformation("Tìm thấy container '{ContainerType}' cho tag '{Tag}'", container.LocalName, tag);
                        break;
                    }
                    // Nếu không, đi lên một cấp nữa
                    parent = parent.Parent;
                }

                if (container == null)
                {
                    _logger.LogWarning($"Tìm thấy tag '{tag}' nhưng không thể tìm thấy container (Shape/GraphicFrame).");
                    continue;
                }

                // 3. Tìm TextBody BÊN TRONG container đó
                // Sửa lỗi: Phải tìm cả P.TextBody (cho Shape) và D.TextBody (cho GraphicFrame)
                OpenXmlCompositeElement textBody = container.Descendants<P.TextBody>().FirstOrDefault() as OpenXmlCompositeElement
                                               ?? container.Descendants<D.TextBody>().FirstOrDefault() as OpenXmlCompositeElement;


                if (textBody == null)
                {
                    _logger.LogWarning($"Tìm thấy container cho tag '{tag}' nhưng không tìm thấy TextBody (P hoặc D) bên trong.");
                    continue;
                }

                _logger.LogInformation("Đã tìm thấy TextBody. Bắt đầu ghi đè text for tag '{Tag}'", tag);

                // 4. Lấy định dạng cũ và Ghi đè text
                var firstPara = textBody.GetFirstChild<D.Paragraph>();
                var paraProps = (D.ParagraphProperties)firstPara?.ParagraphProperties?.CloneNode(true) ?? new D.ParagraphProperties();
                var firstRun = firstPara?.GetFirstChild<D.Run>();
                var runProps = (D.RunProperties)firstRun?.RunProperties?.CloneNode(true) ?? new D.RunProperties();

                // Xóa tất cả Paragraphs cũ
                textBody.RemoveAllChildren<D.Paragraph>();

                var lines = (newText ?? string.Empty).Split('\n');
                if (!lines.Any() || (lines.Length == 1 && string.IsNullOrEmpty(lines[0])))
                {
                    lines = new string[] { "" }; // Thêm một dòng trống để giữ định dạng
                }

                foreach (var line in lines)
                {
                    var newRun = new D.Run(
                        (D.RunProperties)runProps.CloneNode(true),
                        new D.Text(line)
                    );

                    var newPara = new D.Paragraph(
                        (D.ParagraphProperties)paraProps.CloneNode(true),
                        newRun
                    );

                    textBody.Append(newPara);
                }
            }
        }


        private SlidePart CloneSlidePart(PresentationPart presentationPart, SlidePart templateSlidePart, ref uint uniqueSlideId)
        {
            string newRelId = "rId" + Guid.NewGuid().ToString("N");
            var newSlidePart = presentationPart.AddNewPart<SlidePart>(newRelId);

            using (Stream templateStream = templateSlidePart.GetStream())
            using (Stream newSlideStream = newSlidePart.GetStream(FileMode.Create))
            {
                templateStream.CopyTo(newSlideStream);
            }

            if (templateSlidePart.SlideLayoutPart == null)
            {
                throw new InvalidOperationException("Slide mẫu (template slide) không có SlideLayoutPart.");
            }
            string layoutRelId = templateSlidePart.GetIdOfPart(templateSlidePart.SlideLayoutPart);
            newSlidePart.AddPart(templateSlidePart.SlideLayoutPart, layoutRelId);

            foreach (var part in templateSlidePart.Parts)
            {
                if (part.OpenXmlPart is SlideLayoutPart)
                {
                    continue;
                }

                newSlidePart.AddPart(part.OpenXmlPart, part.RelationshipId);
            }

            return newSlidePart;
        }

        #endregion
    }
}