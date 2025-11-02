using MathSlides.Business_Object.Models.Entities;
using MathSlides.Data_Analysis_Object;
using MathSlides.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathSlides.Repository.Repositories
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly MathSlidesAuthDbContext _context;

        public TemplateRepository(MathSlidesAuthDbContext context)
        {
            _context = context;
        }

        public async Task<List<Template>> GetAllTemplatesAsync(bool onlyActive = true)
        {
            var query = _context.Templates.AsQueryable();
            
            if (onlyActive)
            {
                query = query.Where(t => t.IsActive);
            }

            return await query.OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<Template?> GetTemplateByIdAsync(int templateId)
        {
            return await _context.Templates.FindAsync(templateId);
        }

        public async Task<string> GetTemplateContentAsync(int templateId)
        {
            var template = await GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template with ID {templateId} not found");
            }

            // Nếu TemplatePath là đường dẫn file, đọc từ file
            if (!string.IsNullOrEmpty(template.TemplatePath) && File.Exists(template.TemplatePath))
            {
                return await File.ReadAllTextAsync(template.TemplatePath);
            }

            // Nếu không có file, trả về empty hoặc default template
            // Có thể mở rộng để lưu trực tiếp JSON trong database nếu cần
            return "{}";
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            _context.Templates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<Template> UpdateTemplateAsync(Template template)
        {
            _context.Templates.Update(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            var template = await GetTemplateByIdAsync(templateId);
            if (template == null)
            {
                return false;
            }

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}

