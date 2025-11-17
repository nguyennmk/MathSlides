using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Service.DTOs.Generation;
using System.Collections.Generic; 
using System.IO;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface IPowerpointService
    {
        Task<PowerpointImportResponse> ImportPowerpointAsync(Stream fileStream, string fileName, string? name = null, string? description = null);
        Task<string> SaveTemplatePathAsync(string templatePath, int templateId);
        Task<PowerpointImportResponse> GetPowerpointInfoAsync(string templatePath);
        Task<PowerpointImportResponse> UpdatePowerpointInfoAsync(string templatePath, string jsonContent);
        Task<MemoryStream> GeneratePptxFromJsonTemplateAsync(List<Content> contents, string templateJson, string topicName);
        Task<MemoryStream> GeneratePptxFromPptxTemplateAsync(
            GenerationRequest request, 
            Topic topic,
            List<Content> contentList,
            string templatePptxPath
        );
    }
}