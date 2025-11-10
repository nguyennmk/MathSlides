using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace MathSlides.Service.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;

        private readonly IWebHostEnvironment _env;

        public TemplateService(ITemplateRepository templateRepository, IWebHostEnvironment env)
        {
            _templateRepository = templateRepository;
            _env = env;
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
    }
}

