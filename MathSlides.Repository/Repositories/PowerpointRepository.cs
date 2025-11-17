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
            
            var baseDirectory = Path.Combine(basePath, subDirectory);
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileExtension = Path.GetExtension(fileName);
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var uniqueFileName = $"{baseFileName}_{timestamp}{fileExtension}";
            var filePath = Path.Combine(baseDirectory, uniqueFileName);

            
            await File.WriteAllBytesAsync(filePath, fileContent);

            
            return Path.GetFullPath(filePath);
        }

        public async Task<string> ReadFileAsync(string templatePath, string webRootPath)
        {
            
            string physicalPath;

            if (Path.IsPathRooted(templatePath) && templatePath.StartsWith("/"))
            {
                
                var relativePath = templatePath.TrimStart('/');
                physicalPath = Path.Combine(webRootPath, relativePath);
            }
            else if (Path.IsPathRooted(templatePath))
            {
                
                physicalPath = templatePath;
            }
            else
            {
               
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
            
            string physicalPath;

            if (Path.IsPathRooted(templatePath) && templatePath.StartsWith("/"))
            {
                
                var relativePath = templatePath.TrimStart('/');
                physicalPath = Path.Combine(webRootPath, relativePath);
            }
            else if (Path.IsPathRooted(templatePath))
            {
                
                physicalPath = templatePath;
            }
            else
            {
                
                physicalPath = Path.Combine(webRootPath, "Templates", templatePath);
            }

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException($"File không tồn tại: {templatePath}");
            }

            
            await File.WriteAllTextAsync(physicalPath, jsonContent);

            return jsonContent;
        }
    }
}

