using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.DTOs.Admin;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MathSlides.Service.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;

        private readonly IPowerpointService _powerpointService;
        private readonly IWebHostEnvironment _env;

        public TemplateService(ITemplateRepository templateRepository, IWebHostEnvironment env, IPowerpointService powerpointService)
        {
            _templateRepository = templateRepository;
            _env = env;
            _powerpointService = powerpointService;
        }

        public async Task<List<TemplateDTO>> GetAllTemplatesAsync(bool onlyActive = true)
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

            try
            {
                var jsonContent = await _templateRepository.GetTemplateContentAsync(templateId);
                
                if (!string.IsNullOrEmpty(jsonContent) && jsonContent != "{}")
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    templateDTO.Content = JsonSerializer.Deserialize<ImportGDPTRequest>(jsonContent, options);
                }
            }
            catch
            {
                templateDTO.Content = null;
            }

            return templateDTO;
        }

        public async Task<string> GetTemplateJsonAsync(string templateName)
        {
            var wwwRootPath = _env.WebRootPath;

            var templatePath = Path.Combine(wwwRootPath, "templates", templateName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Không tìm thấy file JSON template.", templatePath);
            }

            return await File.ReadAllTextAsync(templatePath);
        }
        public async Task<TemplateDTO> CreateTemplateAsync(CreateTemplateRequestDTO request)
        {
            // 1. Xử lý file Thumbnail
            string thumbnailUrl = await SaveFileAsync(request.ThumbnailFile, "thumbnails");

            // 2. Xử lý file PPTX (Convert -> JSON và lưu)
            PowerpointImportResponse importResult;
            await using (var stream = request.PptxFile.OpenReadStream())
            {
                importResult = await _powerpointService.ImportPowerpointAsync(
                    stream,
                    request.PptxFile.FileName,
                    request.Name,
                    request.Description
                );
            }

            // 3. Tạo Entity Template
            var template = new Template
            {
                Name = request.Name,
                Description = request.Description,
                ThumbnailUrl = thumbnailUrl, // Đường dẫn tương đối của ảnh thumbnail
                TemplatePath = importResult.TemplatePath, // Đường dẫn tương đối của file JSON
                TemplateType = request.TemplateType,
                Tags = request.Tags,
                IsActive = true
            };

            // 4. Lưu vào DB
            var createdTemplate = await _templateRepository.CreateTemplateAsync(template);

            return MapTemplateToDTO(createdTemplate);
        }

        // === SỬA LOGIC CẬP NHẬT TEMPLATE ===
        public async Task<TemplateDTO> UpdateTemplateAsync(int templateId, UpdateTemplateRequestDTO request)
        {
            var template = await _templateRepository.GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template with ID {templateId} not found.");
            }

            // 1. Cập nhật metadata
            template.Name = request.Name;
            template.Description = request.Description;
            template.TemplateType = request.TemplateType;
            template.Tags = request.Tags;
            template.IsActive = request.IsActive;

            // 2. Nếu có file Thumbnail mới
            if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
            {
                DeleteFile(template.ThumbnailUrl); // Xóa file thumbnail cũ
                template.ThumbnailUrl = await SaveFileAsync(request.ThumbnailFile, "thumbnails"); // Lưu file mới
            }

            // 3. Nếu có file PPTX mới
            if (request.PptxFile != null && request.PptxFile.Length > 0)
            {
                DeleteFile(template.TemplatePath); // Xóa file JSON template cũ

                PowerpointImportResponse importResult;
                await using (var stream = request.PptxFile.OpenReadStream())
                {
                    importResult = await _powerpointService.ImportPowerpointAsync(
                        stream,
                        request.PptxFile.FileName,
                        request.Name,
                        request.Description
                    );
                }
                template.TemplatePath = importResult.TemplatePath; // Cập nhật đường dẫn JSON mới
            }

            // 4. Lưu thay đổi vào DB
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

            // 1. Xóa file Thumbnail
            DeleteFile(template.ThumbnailUrl);

            // 2. Xóa file JSON Template
            DeleteFile(template.TemplatePath);

            // 3. Xóa record trong DB
            return await _templateRepository.DeleteTemplateAsync(templateId);
        }

        // --- HÀM HELPER MỚI ĐỂ LƯU FILE ---

        /// <summary>
        /// Lưu file vào một thư mục con trong wwwroot và trả về đường dẫn tương đối.
        /// </summary>
        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            var wwwRootPath = _env.WebRootPath;
            var folderPath = Path.Combine(wwwRootPath, subfolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Tạo tên file unique
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var physicalPath = Path.Combine(folderPath, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về đường dẫn tương đối (ví dụ: /thumbnails/abc.png)
            return $"/{subfolder}/{uniqueFileName}";
        }

        /// <summary>
        /// Xóa file dựa trên đường dẫn tương đối từ wwwroot.
        /// </summary>
        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            try
            {
                var physicalPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
            }
        }

        // Hàm helper map
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
    }
}

