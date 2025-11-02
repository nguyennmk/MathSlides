using MathSlides.Business_Object.Models.Entities;

namespace MathSlides.Repository.Interfaces
{
    public interface IPowerpointRepository
    {
        Task<Template?> GetTemplateByIdAsync(int templateId);
        Task<Template> UpdateTemplatePathAsync(int templateId, string templatePath);
        Task<string> SaveFileAsync(byte[] fileContent, string fileName, string basePath, string subDirectory = "Templates");
        Task<string> ReadFileAsync(string templatePath, string webRootPath);
        Task<string> UpdateFileAsync(string templatePath, string jsonContent, string webRootPath);
    }
}


