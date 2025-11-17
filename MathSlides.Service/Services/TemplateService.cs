    using MathSlides.Business_Object.Models.DTOs.GDPT;
    using MathSlides.Business_Object.Models.DTOs.Powerpoint;
    using MathSlides.Business_Object.Models.Entities;
    using MathSlides.Repository.Interfaces;
    using MathSlides.Service.DTOs.Admin;
    using MathSlides.Service.Interfaces;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    namespace MathSlides.Service.Services
    {
        public class TemplateService : ITemplateService
        {
            private readonly ITemplateRepository _templateRepository;
            private readonly IPowerpointService _powerpointService;
            private readonly IWebHostEnvironment _env;
            private readonly ILogger<TemplateService> _logger;

            public TemplateService(ITemplateRepository templateRepository, IWebHostEnvironment env, IPowerpointService powerpointService, ILogger<TemplateService> logger)
            {
                _templateRepository = templateRepository;
                _env = env;
                _powerpointService = powerpointService;
                _logger = logger;
            }

            public async Task<List<TemplateDTO>> GetAllTemplatesAsync(bool? onlyActive)
            {
                var templates = await _templateRepository.GetAllTemplatesAsync(onlyActive);

                return templates.Select(t => new TemplateDTO
                {
                    TemplateID = t.TemplateID,
                    Name = t.Name,
                    Description = t.Description,
                    ThumbnailUrl = t.ThumbnailUrl,
                    TemplateType = t.TemplateType,
                    Tags = t.Tags,
                    IsActive = t.IsActive
                }).ToList();
            }

            public async Task<TemplateDetailDTO?> GetTemplateByIdAsync(int templateId)
            {
                var template = await _templateRepository.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    return null;
                }

                var templateDTO = new TemplateDetailDTO
                {
                    TemplateID = template.TemplateID,
                    Name = template.Name,
                    Description = template.Description,
                    ThumbnailUrl = template.ThumbnailUrl,
                    TemplateType = template.TemplateType,
                    Tags = template.Tags,
                    IsActive = template.IsActive
                };
                templateDTO.Content = null;

                return templateDTO;
            }

            public async Task<string> GetTemplateJsonAsync(string templateName)
            {
                var wwwRootPath = _env.WebRootPath;
                var templatePath = Path.Combine(wwwRootPath, "templates", templateName);

                if (!File.Exists(templatePath))
                {
                    if (templatePath.EndsWith(".pptx"))
                        return "File is binary PPTX";

                    throw new FileNotFoundException("Không tìm thấy file template.", templatePath);
                }

                return await File.ReadAllTextAsync(templatePath);
            }

            public async Task<TemplateDTO> CreateTemplateAsync(CreateTemplateRequestDTO request)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Tên (Name) không được để trống.");

                string pptxTemplatePath = await SaveNamedFileAsync(request.PptxFile, "Templates", request.Name);

                string thumbnailUrl = "";
                if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
                {
                    string thumbNameBase = $"{request.Name}_Thumbnails";
                    thumbnailUrl = await SaveNamedFileAsync(request.ThumbnailFile, "Thumbnails", thumbNameBase);
                }

                var template = new Template
                {
                    Name = request.Name,
                    Description = request.Description,
                    ThumbnailUrl = thumbnailUrl,
                    TemplatePath = pptxTemplatePath,
                    TemplateType = request.TemplateType,
                    Tags = request.Tags,
                    IsActive = true
                };

                var createdTemplate = await _templateRepository.CreateTemplateAsync(template);

                return MapTemplateToDTO(createdTemplate);
            }

            public async Task<TemplateDTO> UpdateTemplateAsync(int templateId, UpdateTemplateRequestDTO request)
            {
                var template = await _templateRepository.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    throw new KeyNotFoundException($"Template with ID {templateId} not found.");
                }
                template.Name = request.Name;
                template.Description = request.Description;
                template.TemplateType = request.TemplateType;
                template.Tags = request.Tags;
                template.IsActive = request.IsActive;

                if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
                {
                    DeleteFile(template.ThumbnailUrl);

                    string thumbNameBase = $"{request.Name}_Thumbnails";
                    template.ThumbnailUrl = await SaveNamedFileAsync(request.ThumbnailFile, "Thumbnails", thumbNameBase);
                }

                if (request.PptxFile != null && request.PptxFile.Length > 0)
                {
                    DeleteFile(template.TemplatePath);

                    template.TemplatePath = await SaveNamedFileAsync(request.PptxFile, "Templates", request.Name);
                }

                var updatedTemplate = await _templateRepository.UpdateTemplateAsync(template);
                return MapTemplateToDTO(updatedTemplate);
            }

            public async Task<bool> DeleteTemplateAsync(int templateId)
            {
                var template = await _templateRepository.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    return false;
                }

                DeleteFile(template.ThumbnailUrl);
                DeleteFile(template.TemplatePath);

                return await _templateRepository.DeleteTemplateAsync(templateId);
            }

            public async Task<TemplateDTO> ImportPptxAsync(PowerpointImportRequest request)
            {
                if (request.File == null || request.File.Length == 0)
                    throw new ArgumentException("File không được để trống");

                var allowedExtensions = new[] { ".pptx", ".ppt" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Chỉ chấp nhận file PowerPoint (.pptx, .ppt)");

                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Tên (Name) không được để trống.");

                string pptxTemplatePath = await SaveNamedFileAsync(request.File, "Templates", request.Name);

                string thumbnailUrl = "";
                if (request.ThumbnailImage != null && request.ThumbnailImage.Length > 0)
                {
                    var allowedImgExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var imgExt = Path.GetExtension(request.ThumbnailImage.FileName).ToLowerInvariant();
                    if (!allowedImgExtensions.Contains(imgExt))
                    {
                        DeleteFile(pptxTemplatePath);
                        throw new ArgumentException("File Thumbnail chỉ chấp nhận ảnh (.jpg, .png, .gif, .webp)");
                    }

                    string thumbNameBase = $"{request.Name}_Thumbnails";
                    thumbnailUrl = await SaveNamedFileAsync(request.ThumbnailImage, "Thumbnails", thumbNameBase);
                }

                var template = new Template
                {
                    Name = request.Name,
                    Description = request.Description,
                    TemplatePath = pptxTemplatePath,
                    ThumbnailUrl = thumbnailUrl,
                    TemplateType = "PowerPoint",
                    Tags = request.Tags,
                    IsActive = true
                };

                var createdTemplate = await _templateRepository.CreateTemplateAsync(template);
                return MapTemplateToDTO(createdTemplate);
            }

            private void DeleteFile(string relativePath)
            {
                if (string.IsNullOrEmpty(relativePath)) return;
                try
                {
                    var wwwRootPath = _env.WebRootPath;
                    if (string.IsNullOrEmpty(wwwRootPath))
                    {
                        wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    }

                    var physicalPath = Path.Combine(wwwRootPath, relativePath.TrimStart('/'));

                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                        _logger.LogInformation($"Deleted file: {physicalPath}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting file: {relativePath}");
                }
            }

            private TemplateDTO MapTemplateToDTO(Template t)
            {
                return new TemplateDTO
                {
                    TemplateID = t.TemplateID,
                    Name = t.Name,
                    Description = t.Description,
                    ThumbnailUrl = t.ThumbnailUrl,
                    TemplateType = t.TemplateType,
                    Tags = t.Tags,
                    IsActive = t.IsActive
                };
            }

            private string SanitizeFileName(string baseName)
            {
                if (string.IsNullOrEmpty(baseName)) return string.Empty;

                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitizedName = new string(baseName.Where(ch => !invalidChars.Contains(ch)).ToArray());
                sanitizedName = sanitizedName.Replace(" ", "_");
                sanitizedName = Regex.Replace(sanitizedName, @"\.+", ".");
                return sanitizedName.Trim('_', '.', '-');
            }

            private async Task<string> SaveNamedFileAsync(IFormFile file, string subfolder, string baseFileName)
            {
                var wwwRootPath = _env.WebRootPath;
                if (string.IsNullOrEmpty(wwwRootPath))
                {
                    wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var folderPath = Path.Combine(wwwRootPath, subfolder);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var sanitizedBaseName = SanitizeFileName(baseFileName);
                var fileExtension = Path.GetExtension(file.FileName);

                if (string.IsNullOrWhiteSpace(sanitizedBaseName))
                {
                    sanitizedBaseName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                    if (string.IsNullOrWhiteSpace(sanitizedBaseName))
                    {
                        sanitizedBaseName = Guid.NewGuid().ToString();
                    }
                }

                var finalFileName = $"{sanitizedBaseName}{fileExtension}";
                var physicalPath = Path.Combine(folderPath, finalFileName);

                if (File.Exists(physicalPath))
                {
                    _logger.LogWarning("File với tên {FileName} đã tồn tại.", finalFileName);
                    throw new ArgumentException($"Một file với tên '{finalFileName}' đã tồn tại trong thư mục '{subfolder}'. Vui lòng chọn tên khác.");
                }

                await using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativeUrl = $"/{subfolder}/{finalFileName}";
                _logger.LogInformation($"File saved to: {relativeUrl}");
                return relativeUrl;
            }

            private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
            {
                return await SaveNamedFileAsync(file, subfolder, Guid.NewGuid().ToString());
            }
        public async Task<(string FilePath, string ContentType, string FileName)> GetTemplateFileForDownloadAsync(int templateId)
        {
            var template = await _templateRepository.GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template với ID {templateId} không tồn tại.");
            }

            if (string.IsNullOrEmpty(template.TemplatePath))
            {
                throw new FileNotFoundException("Template này chưa có file đính kèm.");
            }

            var wwwRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(wwwRootPath))
            {
                wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var relativePath = template.TemplatePath.TrimStart('/', '\\');
            var physicalPath = Path.Combine(wwwRootPath, relativePath);

            if (!File.Exists(physicalPath))
            {
                _logger.LogError($"File not found on disk: {physicalPath}");
                throw new FileNotFoundException("File không tồn tại trên hệ thống.", template.TemplatePath);
            }

            string contentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            string downloadFileName = $"{template.Name}.pptx"; 

            var extension = Path.GetExtension(physicalPath);
            if (!downloadFileName.EndsWith(extension))
            {
                downloadFileName += extension;
            }

            return (physicalPath, contentType, downloadFileName);
        }
    }
}
    