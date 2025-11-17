using MathSlides.Business_Object.Models.Entities;
using MathSlides.Data_Analysis_Object;
using MathSlides.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathSlides.Repository.Repositories
{
    public class GDPTRepository : IGDPTRepository
    {
        private readonly MathSlidesAuthDbContext _context;

        public GDPTRepository(MathSlidesAuthDbContext context)
        {
            _context = context;
        }

        // Strand operations
        public async Task<Strand?> GetStrandByNameAsync(string name)
        {
            return await _context.Strands
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<Strand> CreateStrandAsync(Strand strand)
        {
            _context.Strands.Add(strand);
            await _context.SaveChangesAsync();
            return strand;
        }

        public async Task<Strand?> GetOrCreateStrandAsync(string name)
        {
            var strand = await GetStrandByNameAsync(name);
            if (strand == null)
            {
                strand = new Strand { Name = name };
                strand = await CreateStrandAsync(strand);
            }
            return strand;
        }

        // Grade operations
        public async Task<Grade?> GetGradeByNameAsync(string name)
        {
            return await _context.Grades
                .FirstOrDefaultAsync(g => g.Name == name);
        }

        public async Task<Grade> CreateGradeAsync(Grade grade)
        {
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return grade;
        }

        public async Task<Grade?> GetOrCreateGradeAsync(string name, int level)
        {
            var grade = await GetGradeByNameAsync(name);
            if (grade == null)
            {
                grade = new Grade { Name = name, Level = level };
                grade = await CreateGradeAsync(grade);
            }
            return grade;
        }

        // Class operations
        public async Task<Class?> GetClassByNameAndGradeAsync(string className, int gradeID)
        {
            return await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == className && c.GradeID == gradeID);
        }

        public async Task<Class> CreateClassAsync(Class classEntity)
        {
            _context.Classes.Add(classEntity);
            await _context.SaveChangesAsync();
            return classEntity;
        }

        public async Task<Class?> GetOrCreateClassAsync(string className, int gradeID)
        {
            var classEntity = await GetClassByNameAndGradeAsync(className, gradeID);
            if (classEntity == null)
            {
                classEntity = new Class { Name = className, GradeID = gradeID };
                classEntity = await CreateClassAsync(classEntity);
            }
            return classEntity;
        }

        // Topic operations
        public async Task<Topic?> GetTopicByNameAsync(string name, int classID)
        {
            return await _context.Topics
                .FirstOrDefaultAsync(t => t.Name == name && t.ClassID == classID);
        }

        public async Task<Topic> CreateTopicAsync(Topic topic)
        {
            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();
            return topic;
        }

        public async Task<TopicVersion> CreateTopicVersionAsync(TopicVersion version)
        {
            _context.TopicVersions.Add(version);
            await _context.SaveChangesAsync();
            return version;
        }

        // Content operations
        public async Task<Content> CreateContentAsync(Content content)
        {
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
            return content;
        }

        // Formula operations
        public async Task<Formula> CreateFormulaAsync(Formula formula)
        {
            _context.Formulas.Add(formula);
            await _context.SaveChangesAsync();
            return formula;
        }

        // Example operations
        public async Task<Example> CreateExampleAsync(Example example)
        {
            _context.Examples.Add(example);
            await _context.SaveChangesAsync();
            return example;
        }

        // Media operations
        public async Task<Media> CreateMediaAsync(Media media)
        {
            _context.Media.Add(media);
            await _context.SaveChangesAsync();
            return media;
        }

        // Curriculum operations
        public async Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className)
        {
            return await _context.Topics
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Grade)
                .Include(t => t.Strand)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Formulas)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Examples)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Media)
                .Where(t => t.Class.Grade.Name == gradeName && t.Class.Name == className)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        public async Task<List<Topic>> GetTopicsByGradeAndClassAsync(string gradeName, string className, bool? isActive)
        {
            var query = _context.Topics
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Grade)
                .Include(t => t.Strand)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Formulas)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Examples)
                .Include(t => t.Contents)
                    .ThenInclude(c => c.Media)
                .AsQueryable();

            query = query.Where(t =>
                t.Class.Grade.Name == gradeName &&
                t.Class.Name == className);

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }
            return await query
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        // Grade and Class operations for selection
        public async Task<List<Grade>> GetAllGradesAsync()
        {
            return await _context.Grades
                .OrderBy(g => g.Level)
                .ThenBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<List<Class>> GetClassesByGradeIdAsync(int gradeId)
        {
            return await _context.Classes
                .Include(c => c.Grade)
                .Where(c => c.GradeID == gradeId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Class>> GetClassesByGradeNameAsync(string gradeName)
        {
            return await _context.Classes
                .Include(c => c.Grade)
                .Where(c => c.Grade.Name == gradeName)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Class>> GetAllClassesAsync()
        {
            return await _context.Classes
                .Include(c => c.Grade)
                .OrderBy(c => c.Grade.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public async Task<Topic?> GetTopicByIdAsync(int topicId)
        {
            return await _context.Topics
                .Include(t => t.Class)
                    .ThenInclude(c => c.Grade)
                .Include(t => t.Strand)
                .FirstOrDefaultAsync(t => t.TopicID == topicId);
        }

        public async Task<Topic> UpdateTopicAsync(Topic topic)
        {
            _context.Topics.Update(topic);
            await _context.SaveChangesAsync();
            return topic;
        }
        public async Task<bool> SoftDeleteTopicAsync(int topicId)
        {
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null)
            {
                return false;
            }

            topic.IsActive = false;

            _context.Topics.Update(topic);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

