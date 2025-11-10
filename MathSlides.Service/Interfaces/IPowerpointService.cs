// File: MathSlides.Service/Interfaces/IPowerpointService.cs
// (THAY THẾ file của bạn)

using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Business_Object.Models.Entities;
using System.Collections.Generic; // Thêm
using System.IO;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface IPowerpointService
    {
        // === CÁC PHƯƠNG THỨC "IMPORT" (CŨ) CỦA BẠN ===
        Task<PowerpointImportResponse> ImportPowerpointAsync(Stream fileStream, string fileName, string? name = null, string? description = null);
        Task<string> SaveTemplatePathAsync(string templatePath, int templateId);
        Task<PowerpointImportResponse> GetPowerpointInfoAsync(string templatePath);
        Task<PowerpointImportResponse> UpdatePowerpointInfoAsync(string templatePath, string jsonContent);

        // === PHƯƠNG THỨC "GENERATE" (MỚI) ===
        // (Đây là phương thức mà SlideGenerationService đang tìm kiếm)
        Task<MemoryStream> GeneratePptxFromJsonTemplateAsync(
            List<Content> contentList,
            string templateJson,
            string topicName
        );
    }
}