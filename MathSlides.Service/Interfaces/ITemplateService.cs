using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Service.DTOs.Admin;

namespace MathSlides.Service.Interfaces
{
    public interface ITemplateService
    {
        Task<List<TemplateDTO>> GetAllTemplatesAsync(bool onlyActive = true);
        Task<TemplateDetailDTO?> GetTemplateByIdAsync(int templateId);

        Task<string> GetTemplateJsonAsync(string templateName);
        Task<TemplateDTO> CreateTemplateAsync(CreateTemplateRequestDTO request);
        Task<TemplateDTO> UpdateTemplateAsync(int templateId, UpdateTemplateRequestDTO request);
        Task<bool> DeleteTemplateAsync(int templateId);

        Task<TemplateDTO> ImportPptxAsync(PowerpointImportRequest request);

        Task<(string FilePath, string ContentType, string FileName)> GetTemplateFileForDownloadAsync(int templateId);
    }
}

