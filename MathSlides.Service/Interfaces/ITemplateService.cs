using MathSlides.Business_Object.Models.DTOs.GDPT;

namespace MathSlides.Service.Interfaces
{
    public interface ITemplateService
    {
        Task<List<TemplateDTO>> GetAllTemplatesAsync(bool onlyActive = true);
        Task<TemplateDetailDTO?> GetTemplateByIdAsync(int templateId);

        Task<string> GetTemplateJsonAsync(string templateName);
    }
}

