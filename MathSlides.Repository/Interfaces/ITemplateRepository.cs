using MathSlides.Business_Object.Models.Entities;

namespace MathSlides.Repository.Interfaces
{
    public interface ITemplateRepository
    {
        Task<List<Template>> GetAllTemplatesAsync(bool onlyActive = true);
        Task<Template?> GetTemplateByIdAsync(int templateId);
        Task<string> GetTemplateContentAsync(int templateId);
        Task<Template> CreateTemplateAsync(Template template);
        Task<Template> UpdateTemplateAsync(Template template);
        Task<bool> DeleteTemplateAsync(int templateId);
        Task<int> SaveChangesAsync();
    }
}

