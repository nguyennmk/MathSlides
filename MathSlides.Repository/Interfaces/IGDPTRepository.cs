using MathSlides.Business_Object.Models.Entities;

namespace MathSlides.Repository.Interfaces
{
    public interface IGDPTRepository
    {
        Task<Strand?> GetStrandByNameAsync(string name);
        Task<Strand> CreateStrandAsync(Strand strand);
        Task<Strand?> GetOrCreateStrandAsync(string name);

        Task<Grade?> GetGradeByNameAsync(string name);
        Task<Grade> CreateGradeAsync(Grade grade);
        Task<Grade?> GetOrCreateGradeAsync(string name, int level);

        Task<Class?> GetClassByNameAndGradeAsync(string className, int gradeID);
        Task<Class> CreateClassAsync(Class classEntity);
        Task<Class?> GetOrCreateClassAsync(string className, int gradeID);

        Task<Topic?> GetTopicByNameAsync(string name, int classID);
        Task<Topic> CreateTopicAsync(Topic topic);
        Task<TopicVersion> CreateTopicVersionAsync(TopicVersion version);

        Task<Content> CreateContentAsync(Content content);

        Task<Formula> CreateFormulaAsync(Formula formula);

        Task<Example> CreateExampleAsync(Example example);

        Task<Media> CreateMediaAsync(Media media);

        Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className);
        Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className, bool? isActive);

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

