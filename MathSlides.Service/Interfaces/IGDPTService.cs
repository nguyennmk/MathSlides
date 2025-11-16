using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Service.DTOs.GDPT;

namespace MathSlides.Service.Interfaces
{
    public interface IGDPTService
    {
        Task<ImportGDPTResponse> ImportGDPTDataAsync(ImportGDPTRequest request);
        Task<List<CurriculumDTO>> GetCurriculumByGradeAndClassAsync(string gradeName, string className);
        Task<List<GradeDTO>> GetAllGradesWithClassesAsync();
        Task<List<ClassDTO>> GetClassesByGradeIdAsync(int gradeId);
        Task<List<ClassDTO>> GetClassesByGradeNameAsync(string gradeName);
        Task<List<ClassDTO>> GetAllClassesAsync();
        Task<CurriculumDTO> UpdateTopicAsync(int topicId, UpdateTopicRequestDTO request);
    }
}

