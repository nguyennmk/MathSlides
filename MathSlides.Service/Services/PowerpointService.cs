using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using MathSlides.Service.DTOs.Generation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace MathSlides.Service.Services
{
    public class PowerpointService : IPowerpointService
    {
        #region Original Import/Management Functions (Không thay đổi)
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
                    Texts = new List<string>()
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

        private class SlideData { public string SlideId { get; set; } = string.Empty; public List<ShapeData> Shapes { get; set; } = new(); public List<string> Texts { get; set; } = new(); }
        private class ShapeData { public uint ShapeId { get; set; } public string Name { get; set; } = string.Empty; public string Text { get; set; } = string.Empty; }
        #endregion

        #region JSON Template Generation (Không thay đổi)

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

        #region PPTX Template Generation (Tagging Method)

        public async Task<MemoryStream> GeneratePptxFromPptxTemplateAsync(
            GenerationRequest request,
            Topic topic,
            List<Content> contentList,
            string templatePptxPath)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (contentList == null || contentList.Count < 2)
                throw new ArgumentException("ContentList phải chứa ít nhất 2 mục (cho Slide 2 và 3)", nameof(contentList));

            if (!File.Exists(templatePptxPath))
            {
                _logger.LogError("Không tìm thấy file template .pptx tại đường dẫn: {Path}", templatePptxPath);
                throw new FileNotFoundException("Không tìm thấy file template .pptx", templatePptxPath);
            }

            _logger.LogInformation("Đang sao chép file template vào MemoryStream...");
            var ms = new MemoryStream();
            using (var fs = new FileStream(templatePptxPath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(ms);
            }
            ms.Position = 0;

            using (PresentationDocument pptDoc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = pptDoc.PresentationPart;
                if (presentationPart?.Presentation?.SlideIdList == null)
                {
                    throw new InvalidOperationException("File template .pptx không hợp lệ.");
                }

                _logger.LogInformation("--- Bắt đầu tạo Dictionary tổng hợp cho TẤT CẢ các tag ---");

                var content1 = contentList[0];
                var content2 = contentList[1];
                var allReplacements = new Dictionary<string, string>
                {
                    { "a1", request.Name ?? topic.Name ?? "" },
                    { "a2", request.Title ?? "" },
                    { "a3", topic.Class?.Name ?? "" },
                    { "a4", request.Objectives ?? topic.Objectives ?? "" },
                    { "b1", content1.Title ?? "" },
                    { "b2", content1.Summary ?? "" },
                    { "b3", FormatMathContent(content1.Formulas.FirstOrDefault()?.FormulaText) },
                    { "b4", FormatMathContent(content1.Examples.FirstOrDefault()?.ExampleText) },
                    { "c1", content2.Title ?? "" },
                    { "c2", content2.Summary ?? "" },
                    { "c3", FormatMathContent(content2.Formulas.FirstOrDefault()?.FormulaText) }
                };

                _logger.LogInformation("Đã tạo Dictionary với {Count} tags. Bắt đầu quét slides...", allReplacements.Count);

                var slideParts = presentationPart.SlideParts.ToList();
                if (slideParts.Count == 0)
                {
                    _logger.LogWarning("File template không chứa slide nào.");
                    throw new InvalidOperationException("File template không có slide.");
                }

                int slideIndex = 1;
                foreach (var slidePart in slideParts)
                {
                    _logger.LogInformation("--- Đang quét Slide {Index}... ---", slideIndex++);
                    ReplacePlaceholdersByTag(slidePart, allReplacements);
                }

                _logger.LogInformation("Đã xử lý tất cả các slide. Đang lưu file...");
                presentationPart.Presentation.Save();
            }

            ms.Position = 0;
            return ms;
        }

        private void ReplacePlaceholdersByTag(SlidePart slidePart, Dictionary<string, string> replacements)
        {
            if (slidePart?.Slide == null) return;

            var textRuns = slidePart.Slide.Descendants<D.Run>().ToList();

            foreach (var run in textRuns)
            {
                if (run.Text == null || string.IsNullOrEmpty(run.Text.Text)) continue;

                foreach (var replacement in replacements)
                {
                    string tag = replacement.Key;
                    string value = replacement.Value ?? "";

                    if (run.Text.Text.Contains(tag))
                    {
                        _logger.LogDebug("Tìm thấy tag: {Tag} trong run text: {RunText}", tag, run.Text.Text);

                        if (value.Contains("\n"))
                        {
                            var lines = value.Split(new[] { '\n' }, StringSplitOptions.None);
                            run.Text.Text = run.Text.Text.Replace(tag, lines[0]);

                            var parentParagraph = run.Parent as D.Paragraph;
                            if (parentParagraph != null)
                            {
                                D.RunProperties currentRunProperties = run.RunProperties?.CloneNode(true) as D.RunProperties;

                                OpenXmlElement lastAddedElement = run;

                                for (int i = 1; i < lines.Length; i++)
                                {
                                    var newBreak = new D.Break();
                                    parentParagraph.InsertAfter(newBreak, lastAddedElement);
                                    lastAddedElement = newBreak;

                                    var newRun = new D.Run();
                                    if (currentRunProperties != null)
                                    {
                                        newRun.Append(currentRunProperties.CloneNode(true));
                                    }
                                    newRun.Append(new D.Text(lines[i]));

                                    parentParagraph.InsertAfter(newRun, lastAddedElement);
                                    lastAddedElement = newRun;
                                }
                                _logger.LogInformation("Đã thay thế tag {Tag} bằng {LineCount} dòng.", tag, lines.Length);
                            }
                        }
                        else
                        {
                            run.Text.Text = run.Text.Text.Replace(tag, value);
                            _logger.LogInformation("Đã thay thế tag {Tag} bằng giá trị đơn.", tag);
                        }
                    }
                }
            }
        }

        private SlidePart CloneSlidePart(PresentationPart presentationPart, SlidePart templateSlide, ref uint uniqueSlideId)
        {
            _logger.LogError("CloneSlidePart không nên được gọi trong luồng này.");
            throw new InvalidOperationException("Logic clone slide không còn được sử dụng.");
        }

        private static readonly Regex FractionRegex = new(@"\\frac\{([^}]+)\}\{([^}]+)\}", RegexOptions.Compiled);

        private string FormatMathContent(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string output = input;

            while (FractionRegex.IsMatch(output))
            {
                output = FractionRegex.Replace(output, match =>
                {
                    string numerator = match.Groups[1].Value;
                    string denominator = match.Groups[2].Value;
                    
                    numerator = FormatFractionRecursive(numerator);
                    denominator = FormatFractionRecursive(denominator);
                    
                    bool numeratorNeedsParens = numerator.Contains("+") || numerator.Contains("-") || 
                                               numerator.Contains("×") || numerator.Contains("/") ||
                                               numerator.Contains(" ");
                    bool denominatorNeedsParens = denominator.Contains("+") || denominator.Contains("-") ||
                                                 denominator.Contains("×") || denominator.Contains("/") ||
                                                 denominator.Contains(" ");
                    
                    if (numeratorNeedsParens && denominatorNeedsParens)
                    {
                        return $"({numerator})/({denominator})";
                    }
                    else if (numeratorNeedsParens)
                    {
                        return $"({numerator})/{denominator}";
                    }
                    else if (denominatorNeedsParens)
                    {
                        return $"{numerator}/({denominator})";
                    }
                    else
                    {
                        return $"{numerator}/{denominator}";
                    }
                });
            }

            output = output.Replace("\\times", "×");
            output = output.Replace("\\cdot", "·");
            output = output.Replace("\\le", "≤");
            output = output.Replace("\\ge", "≥");
            output = output.Replace("\\neq", "≠");
            output = output.Replace("\\pm", "±");
            output = output.Replace("\\mp", "∓");
            output = output.Replace("\\rightarrow", "→");
            output = output.Replace("\\leftarrow", "←");
            output = output.Replace("\\Rightarrow", "⇒");
            output = output.Replace("\\Leftarrow", "⇐");
            output = output.Replace("\\approx", "≈");
            output = output.Replace("\\equiv", "≡");
            output = output.Replace("\\propto", "∝");
            
            output = output.Replace("\\sqrt", "√");
            output = output.Replace("\\sum", "∑");
            output = output.Replace("\\prod", "∏");
            output = output.Replace("\\int", "∫");
            output = output.Replace("\\infty", "∞");
            output = output.Replace("\\pi", "π");
            output = output.Replace("\\alpha", "α");
            output = output.Replace("\\beta", "β");
            output = output.Replace("\\gamma", "γ");
            output = output.Replace("\\delta", "δ");
            output = output.Replace("\\theta", "θ");
            output = output.Replace("\\lambda", "λ");
            output = output.Replace("\\mu", "μ");
            output = output.Replace("\\sigma", "σ");
            output = output.Replace("\\phi", "φ");
            output = output.Replace("\\omega", "ω");

            output = output.Replace("\\leq", "≤");
            output = output.Replace("\\geq", "≥");
            output = output.Replace("\\sim", "∼");
            output = output.Replace("\\simeq", "≃");
            output = output.Replace("\\cong", "≅");

            output = output.Replace("{", "").Replace("}", "");

            return output.Trim();
        }

        private string FormatFractionRecursive(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string output = input;

            while (FractionRegex.IsMatch(output))
            {
                output = FractionRegex.Replace(output, match =>
                {
                    string numerator = match.Groups[1].Value;
                    string denominator = match.Groups[2].Value;
                    
                    numerator = FormatFractionRecursive(numerator);
                    denominator = FormatFractionRecursive(denominator);
                    
                    bool numeratorNeedsParens = numerator.Contains("+") || numerator.Contains("-") || 
                                               numerator.Contains("×") || numerator.Contains("/");
                    bool denominatorNeedsParens = denominator.Contains("+") || denominator.Contains("-") ||
                                                 denominator.Contains("×") || denominator.Contains("/");
                    
                    if (numeratorNeedsParens && denominatorNeedsParens)
                    {
                        return $"({numerator})/({denominator})";
                    }
                    else if (numeratorNeedsParens)
                    {
                        return $"({numerator})/{denominator}";
                    }
                    else if (denominatorNeedsParens)
                    {
                        return $"{numerator}/({denominator})";
                    }
                    else
                    {
                        return $"{numerator}/{denominator}";
                    }
                });
            }

            output = output.Replace("\\times", "×");
            output = output.Replace("\\cdot", "·");
            
            return output;
        }

        #endregion
    }
}