using MathSlides.Business_Object.Models.Entities;
using MathSlides.Data_Analysis_Object;
using MathSlides.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathSlides.Repository.Repositories
{
    public class PowerpointRepository : IPowerpointRepository
    {
        private readonly MathSlidesAuthDbContext _context;

        public PowerpointRepository(MathSlidesAuthDbContext context)
        {
            _context = context;
        }

        public async Task<Template?> GetTemplateByIdAsync(int templateId)
        {
            return await _context.Templates.FindAsync(templateId);
        }

        public async Task<Template> UpdateTemplatePathAsync(int templateId, string templatePath)
        {
            var template = await GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template with ID {templateId} not found");
            }

            template.TemplatePath = templatePath;
            _context.Templates.Update(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<string> SaveFileAsync(byte[] fileContent, string fileName, string basePath, string subDirectory = "Templates")
        {
            // Tạo thư mục nếu chưa tồn tại
            var baseDirectory = Path.Combine(basePath, subDirectory);
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            // Tạo tên file unique
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileExtension = Path.GetExtension(fileName);
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var uniqueFileName = $"{baseFileName}_{timestamp}{fileExtension}";
            var filePath = Path.Combine(baseDirectory, uniqueFileName);

            // Lưu file
            await File.WriteAllBytesAsync(filePath, fileContent);

            // Trả về đường dẫn tuyệt đối để lưu vào database (tương thích với TemplateRepository)
            return Path.GetFullPath(filePath);
        }

        public async Task<string> ReadFileAsync(string templatePath, string webRootPath)
        {
            // templatePath có thể là relative path (/Templates/file.json) hoặc chỉ tên file
            string physicalPath;

            if (Path.IsPathRooted(templatePath) && templatePath.StartsWith("/"))
            {
                // Relative path: /Templates/file.json -> wwwroot/Templates/file.json
                var relativePath = templatePath.TrimStart('/');
                physicalPath = Path.Combine(webRootPath, relativePath);
            }
            else if (Path.IsPathRooted(templatePath))
            {
                // Absolute path
                physicalPath = templatePath;
            }
            else
            {
                // Chỉ là tên file, tìm trong Templates folder
                physicalPath = Path.Combine(webRootPath, "Templates", templatePath);
            }

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException($"File không tồn tại: {templatePath}");
            }

            return await File.ReadAllTextAsync(physicalPath);
        }

        public async Task<string> UpdateFileAsync(string templatePath, string jsonContent, string webRootPath)
        {
            // templatePath có thể là relative path (/Templates/file.json) hoặc chỉ tên file
            string physicalPath;

            if (Path.IsPathRooted(templatePath) && templatePath.StartsWith("/"))
            {
                // Relative path: /Templates/file.json -> wwwroot/Templates/file.json
                var relativePath = templatePath.TrimStart('/');
                physicalPath = Path.Combine(webRootPath, relativePath);
            }
            else if (Path.IsPathRooted(templatePath))
            {
                // Absolute path
                physicalPath = templatePath;
            }
            else
            {
                // Chỉ là tên file, tìm trong Templates folder
                physicalPath = Path.Combine(webRootPath, "Templates", templatePath);
            }

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException($"File không tồn tại: {templatePath}");
            }

            // Ghi đè nội dung JSON mới
            await File.WriteAllTextAsync(physicalPath, jsonContent);

            return jsonContent;
        }
    }
}

