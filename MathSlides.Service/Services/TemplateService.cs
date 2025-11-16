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
            // 1. Xử lý file Thumbnail (Lưu vào wwwroot/thumbnails)
            string thumbnailUrl = await SaveFileAsync(request.ThumbnailFile, "thumbnails");

            // 2. Xử lý file PPTX (Convert -> JSON và lưu vào wwwroot/templates)
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

            // 2. Nếu có file Thumbnail mới, thay thế file cũ
            if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
            {
                DeleteFile(template.ThumbnailUrl); // Xóa file thumbnail cũ
                template.ThumbnailUrl = await SaveFileAsync(request.ThumbnailFile, "thumbnails"); // Lưu file mới
            }

            // 3. Nếu có file PPTX mới, thay thế file cũ
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

        public async Task<TemplateDTO> ImportPptxAsync(PowerpointImportRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("File không được để trống");
            }

            var allowedExtensions = new[] { ".pptx", ".ppt" };
            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Chỉ chấp nhận file PowerPoint (.pptx, .ppt)");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Tên (Name) không được để trống, vì nó sẽ được dùng làm tên file.");
            }

            // 1. Lưu file PPTX gốc vào /wwwroot/Templates/
            //    SỬ DỤNG HÀM HELPER MỚI (SaveNamedFileAsync)
            //    Chúng ta truyền request.Name để làm tên file
            string pptxTemplatePath = await SaveNamedFileAsync(request.File, "Templates", request.Name);

            // 2. Tạo Entity Template mới
            var template = new Template
            {
                Name = request.Name, // Tên này cũng được lưu vào CSDL
                Description = request.Description,
                TemplatePath = pptxTemplatePath, // Đường dẫn tới file PPTX đã được đặt tên
                ThumbnailUrl = "", // Không có thumbnail trong luồng import này
                TemplateType = "PowerPoint",
                Tags = "",
                IsActive = true
            };

            // 3. Lưu vào DB
            var createdTemplate = await _templateRepository.CreateTemplateAsync(template);

            // 4. Trả về DTO
            return MapTemplateToDTO(createdTemplate);
        }

        // --- HÀM HELPER MỚI ĐỂ LƯU FILE ---

        /// <summary>
        /// Lưu file vào một thư mục con trong wwwroot và trả về đường dẫn tương đối.
        /// </summary>
        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            // Đảm bảo wwwroot tồn tại
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

            // Tạo tên file unique
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var physicalPath = Path.Combine(folderPath, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về đường dẫn tương đối (ví dụ: /thumbnails/abc.png)
            var relativeUrl = $"/{subfolder}/{uniqueFileName}";
            _logger.LogInformation($"File saved to: {relativeUrl}");
            return relativeUrl;
        }

        /// <summary>
        /// Xóa file dựa trên đường dẫn tương đối từ wwwroot.
        /// </summary>
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

        /// <summary>
        /// "Làm sạch" tên file để đảm bảo nó an toàn cho hệ thống file và URL.
        /// </summary>
        private string SanitizeFileName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return string.Empty;

            var invalidChars = Path.GetInvalidFileNameChars();
            // Xóa các ký tự không hợp lệ
            var sanitizedName = new string(baseName.Where(ch => !invalidChars.Contains(ch)).ToArray());

            // Thay thế khoảng trắng bằng gạch dưới (tùy chọn nhưng nên làm)
            sanitizedName = sanitizedName.Replace(" ", "_");

            // Xóa các dấu chấm liên tiếp
            sanitizedName = Regex.Replace(sanitizedName, @"\.+", ".");

            return sanitizedName.Trim('_', '.', '-'); // Xóa các ký tự phân cách ở đầu/cuối
        }

        /// <summary>
        /// (HÀM MỚI) Lưu file với một tên CỤ THỂ, có kiểm tra trùng lặp.
        /// </summary>
        /// <param name="file">File được tải lên</param>
        /// <param name="subfolder">Thư mục con (ví dụ: "Templates")</param>
        /// <param name="baseFileName">Tên file mong muốn (không bao gồm phần mở rộng)</param>
        private async Task<string> SaveNamedFileAsync(IFormFile file, string subfolder, string baseFileName)
        {
            // 1. Lấy đường dẫn thư mục
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

            // 2. Làm sạch tên file và tạo đường dẫn
            var sanitizedBaseName = SanitizeFileName(baseFileName);
            var fileExtension = Path.GetExtension(file.FileName); // e.g., ".pptx"

            // Đảm bảo tên file sạch không bị rỗng
            if (string.IsNullOrWhiteSpace(sanitizedBaseName))
            {
                // Nếu tên rỗng, dùng tạm tên gốc của file
                sanitizedBaseName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                if (string.IsNullOrWhiteSpace(sanitizedBaseName))
                {
                    sanitizedBaseName = Guid.NewGuid().ToString(); // Fallback cuối cùng
                }
            }

            var finalFileName = $"{sanitizedBaseName}{fileExtension}"; // e.g., "Bai_Giang_Phan_So.pptx"
            var physicalPath = Path.Combine(folderPath, finalFileName);

            // 3. KIỂM TRA XEM FILE ĐÃ TỒN TẠI CHƯA
            if (File.Exists(physicalPath))
            {
                _logger.LogWarning("File với tên {FileName} đã tồn tại.", finalFileName);
                // Ném lỗi này sẽ bị bắt bởi catch (ArgumentException ex) trong Controller
                throw new ArgumentException($"Một file với tên '{finalFileName}' đã tồn tại trong thư mục 'Templates'. Vui lòng chọn tên khác.");
            }

            // 4. Lưu file
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Trả về đường dẫn tương đối (ví dụ: /Templates/Bai_Giang_Phan_So.pptx)
            var relativeUrl = $"/{subfolder}/{finalFileName}";
            _logger.LogInformation($"File saved to: {relativeUrl}");
            return relativeUrl;
        }
    }
}

