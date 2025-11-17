using MathSlides.Business_Object.Models.Entities;

namespace MathSlides.Repository.Interfaces
{
    public interface IGDPTRepository
    {
        // Strand operations
        Task<Strand?> GetStrandByNameAsync(string name);
        Task<Strand> CreateStrandAsync(Strand strand);
        Task<Strand?> GetOrCreateStrandAsync(string name);

        // Grade operations
        Task<Grade?> GetGradeByNameAsync(string name);
        Task<Grade> CreateGradeAsync(Grade grade);
        Task<Grade?> GetOrCreateGradeAsync(string name, int level);

        // Class operations
        Task<Class?> GetClassByNameAndGradeAsync(string className, int gradeID);
        Task<Class> CreateClassAsync(Class classEntity);
        Task<Class?> GetOrCreateClassAsync(string className, int gradeID);

        // Topic operations
        Task<Topic?> GetTopicByNameAsync(string name, int classID);
        Task<Topic> CreateTopicAsync(Topic topic);
        Task<TopicVersion> CreateTopicVersionAsync(TopicVersion version);

        // Content operations
        Task<Content> CreateContentAsync(Content content);

        // Formula operations
        Task<Formula> CreateFormulaAsync(Formula formula);

        // Example operations
        Task<Example> CreateExampleAsync(Example example);

        // Media operations
        Task<Media> CreateMediaAsync(Media media);

        // Curriculum operations
        Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className);
        Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className, bool? isActive);

        // Grade and Class operations for selection
        Task<List<Grade>> GetAllGradesAsync();
        Task<List<Class>> GetClassesByGradeIdAsync(int gradeId);
        Task<List<Class>> GetClassesByGradeNameAsync(string gradeName);
        Task<List<Class>> GetAllClassesAsync();

        Task<int> SaveChangesAsync();
        Task<Topic?> GetTopicByIdAsync(int topicId);
        Task<Topic> UpdateTopicAsync(Topic topic);
        Task<bool> SoftDeleteTopicAsync(int topicId);
    }
}

