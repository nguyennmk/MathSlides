using MathSlides.Business_Object.Models.DTOs.Powerpoint;

namespace MathSlides.Service.Interfaces
{
    public interface IPowerpointService
    {
        Task<PowerpointImportResponse> ImportPowerpointAsync(Stream fileStream, string fileName, string? name = null, string? description = null);
        Task<string> SaveTemplatePathAsync(string templatePath, int templateId);
        Task<PowerpointImportResponse> GetPowerpointInfoAsync(string templatePath);
        Task<PowerpointImportResponse> UpdatePowerpointInfoAsync(string templatePath, string jsonContent);
    }
}


