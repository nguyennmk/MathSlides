using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.DTOs.GDPT;
using MathSlides.Service.Interfaces;
using System.Text.Json;

namespace MathSlides.Service.Services
{
    public class GDPTService : IGDPTService
    {
        private readonly IGDPTRepository _gdptRepository;

        public GDPTService(IGDPTRepository gdptRepository)
        {
            _gdptRepository = gdptRepository;
        }

        private CurriculumDTO MapTopicToCurriculumDTO(Topic topic)
        {
            return new CurriculumDTO
            {
                TopicID = topic.TopicID,
                TopicName = topic.Name,
                ClassName = topic.Class?.Name ?? "N/A",
                GradeName = topic.Class?.Grade?.Name ?? "N/A",
                StrandName = topic.Strand?.Name ?? "N/A",
                Objectives = topic.Objectives,
                Source = topic.Source,
                Contents = topic.Contents?.Select(content => new ContentDTO
                {
                    ContentID = content.ContentID,
                    Title = content.Title,
                    Summary = content.Summary,
                    Formulas = content.Formulas.Select(f => new FormulaDTO
                    {
                        FormulaID = f.FormulaID,
                        FormulaText = f.FormulaText,
                        Explanation = f.Explanation
                    }).ToList(),
                    Examples = content.Examples.Select(e => new ExampleDTO
                    {
                        ExampleID = e.ExampleID,
                        ExampleText = e.ExampleText
                    }).ToList(),
                    Media = content.Media.Select(m => new MediaDTO
                    {
                        MediaID = m.MediaID,
                        Type = m.Type,
                        Url = m.Url,
                        Description = m.Description
                    }).ToList()
                }).ToList() ?? new List<ContentDTO>()
            };
        }
        public async Task<ImportGDPTResponse> ImportGDPTDataAsync(ImportGDPTRequest request)
        {
            var response = new ImportGDPTResponse
            {
                Success = true,
                Message = "Import completed successfully",
                Errors = new List<string>()
            };

            try
            {
                foreach (var topicDTO in request.Topics)
                {
                    try
                    {
                        // 1. Lấy hoặc tạo Strand
                        var strand = await _gdptRepository.GetOrCreateStrandAsync(topicDTO.StrandName);
                        if (strand == null)
                        {
                            response.Errors.Add($"Không thể tạo Strand: {topicDTO.StrandName}");
                            continue;
                        }

                        // 2. Lấy hoặc tạo Grade (giả sử Level = 1, 2, 3 dựa trên tên cấp)
                        int gradeLevel = GetGradeLevel(topicDTO.GradeName);
                        var grade = await _gdptRepository.GetOrCreateGradeAsync(topicDTO.GradeName, gradeLevel);
                        if (grade == null)
                        {
                            response.Errors.Add($"Không thể tạo Grade: {topicDTO.GradeName}");
                            continue;
                        }

                        // 3. Lấy hoặc tạo Class
                        var classEntity = await _gdptRepository.GetOrCreateClassAsync(topicDTO.ClassName, grade.GradeID);
                        if (classEntity == null)
                        {
                            response.Errors.Add($"Không thể tạo Class: {topicDTO.ClassName}");
                            continue;
                        }

                        // 4. Kiểm tra Topic đã tồn tại chưa
                        var existingTopic = await _gdptRepository.GetTopicByNameAsync(topicDTO.TopicName, classEntity.ClassID);
                        if (existingTopic != null)
                        {
                            response.Errors.Add($"Topic đã tồn tại: {topicDTO.TopicName} trong {topicDTO.ClassName}");
                            continue;
                        }

                        // 5. Tạo Topic
                        var topic = new Topic
                        {
                            Name = topicDTO.TopicName,
                            ClassID = classEntity.ClassID,
                            StrandID = strand.StrandID,
                            Objectives = topicDTO.Objectives,
                            Source = topicDTO.Source
                        };
                        topic = await _gdptRepository.CreateTopicAsync(topic);
                        response.TotalTopicsImported++;

                        // 6. Tạo TopicVersion nếu có thông tin
                        if (!string.IsNullOrEmpty(topicDTO.Source))
                        {
                            var version = new TopicVersion
                            {
                                TopicID = topic.TopicID,
                                VersionNumber = 1,
                                Changes = $"Nhập từ nguồn: {topicDTO.Source}",
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _gdptRepository.CreateTopicVersionAsync(version);
                        }

                        // 7. Import Contents với các Formulas, Examples, Media
                        foreach (var contentDTO in topicDTO.Contents)
                        {
                            var content = new Content
                            {
                                TopicID = topic.TopicID,
                                Title = contentDTO.Title,
                                Summary = contentDTO.Summary
                            };
                            content = await _gdptRepository.CreateContentAsync(content);
                            response.TotalContentsImported++;

                            // Import Formulas
                            foreach (var formulaDTO in contentDTO.Formulas)
                            {
                                var formula = new Formula
                                {
                                    ContentID = content.ContentID,
                                    FormulaText = formulaDTO.FormulaText,
                                    Explanation = formulaDTO.Explanation
                                };
                                await _gdptRepository.CreateFormulaAsync(formula);
                            }

                            // Import Examples
                            foreach (var exampleDTO in contentDTO.Examples)
                            {
                                var example = new Example
                                {
                                    ContentID = content.ContentID,
                                    ExampleText = exampleDTO.ExampleText
                                };
                                await _gdptRepository.CreateExampleAsync(example);
                            }

                            // Import Media
                            foreach (var mediaDTO in contentDTO.Media)
                            {
                                var media = new Media
                                {
                                    ContentID = content.ContentID,
                                    Type = mediaDTO.Type,
                                    Url = mediaDTO.Url,
                                    Description = mediaDTO.Description
                                };
                                await _gdptRepository.CreateMediaAsync(media);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Errors.Add($"Lỗi khi import topic {topicDTO.TopicName}: {ex.Message}");
                    }
                }

                // Lưu tất cả các thay đổi còn lại
                await _gdptRepository.SaveChangesAsync();

                if (response.Errors.Count > 0)
                {
                    response.Message = $"Import hoàn thành với {response.Errors.Count} lỗi";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi import dữ liệu: {ex.Message}";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<List<CurriculumDTO>> GetCurriculumByGradeAndClassAsync(string gradeName, string className)
        {
            var topics = await _gdptRepository.GetTopicsByGradeAndClassAsync(gradeName, className);

            return topics.Select(topic => new CurriculumDTO
            {
                TopicID = topic.TopicID,
                TopicName = topic.Name,
                ClassName = topic.Class.Name,
                GradeName = topic.Class.Grade.Name,
                StrandName = topic.Strand.Name,
                Objectives = topic.Objectives,
                Source = topic.Source,
                Contents = topic.Contents.Select(content => new ContentDTO
                {
                    ContentID = content.ContentID,
                    Title = content.Title,
                    Summary = content.Summary,
                    Formulas = content.Formulas.Select(f => new FormulaDTO
                    {
                        FormulaID = f.FormulaID,
                        FormulaText = f.FormulaText,
                        Explanation = f.Explanation
                    }).ToList(),
                    Examples = content.Examples.Select(e => new ExampleDTO
                    {
                        ExampleID = e.ExampleID,
                        ExampleText = e.ExampleText
                    }).ToList(),
                    Media = content.Media.Select(m => new MediaDTO
                    {
                        MediaID = m.MediaID,
                        Type = m.Type,
                        Url = m.Url,
                        Description = m.Description
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<List<GradeDTO>> GetAllGradesWithClassesAsync()
        {
            var grades = await _gdptRepository.GetAllGradesAsync();
            
            return grades.Select(grade => new GradeDTO
            {
                GradeID = grade.GradeID,
                Name = grade.Name,
                Level = grade.Level,
                Classes = grade.Classes.Select(c => new ClassDTO
                {
                    ClassID = c.ClassID,
                    Name = c.Name,
                    GradeID = c.GradeID,
                    GradeName = grade.Name
                }).OrderBy(c => c.Name).ToList()
            }).ToList();
        }

        public async Task<List<ClassDTO>> GetClassesByGradeIdAsync(int gradeId)
        {
            var classes = await _gdptRepository.GetClassesByGradeIdAsync(gradeId);
            
            return classes.Select(c => new ClassDTO
            {
                ClassID = c.ClassID,
                Name = c.Name,
                GradeID = c.GradeID,
                GradeName = c.Grade.Name
            }).ToList();
        }

        public async Task<List<ClassDTO>> GetClassesByGradeNameAsync(string gradeName)
        {
            var classes = await _gdptRepository.GetClassesByGradeNameAsync(gradeName);
            
            return classes.Select(c => new ClassDTO
            {
                ClassID = c.ClassID,
                Name = c.Name,
                GradeID = c.GradeID,
                GradeName = c.Grade.Name
            }).ToList();
        }

        public async Task<List<ClassDTO>> GetAllClassesAsync()
        {
            var classes = await _gdptRepository.GetAllClassesAsync();
            
            return classes.Select(c => new ClassDTO
            {
                ClassID = c.ClassID,
                Name = c.Name,
                GradeID = c.GradeID,
                GradeName = c.Grade.Name
            }).ToList();
        }

        private int GetGradeLevel(string gradeName)
        {
            // Hàm helper để xác định Level từ tên cấp học
            if (gradeName.Contains("1") || gradeName.Contains("Tiểu học"))
                return 1;
            if (gradeName.Contains("2") || gradeName.Contains("THCS"))
                return 2;
            if (gradeName.Contains("3") || gradeName.Contains("THPT"))
                return 3;
            return 1; // Default
        }
        public async Task<CurriculumDTO> UpdateTopicAsync(int topicId, UpdateTopicRequestDTO request)
        {
            var topic = await _gdptRepository.GetTopicByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy Topic với ID: {topicId}");
            }

            // Cập nhật các trường
            topic.Name = request.Name;
            topic.Objectives = request.Objectives;
            topic.Source = request.Source;
            topic.ClassID = request.ClassID;
            topic.StrandID = request.StrandID;


            var updatedTopic = await _gdptRepository.UpdateTopicAsync(topic);

            var fullUpdatedTopic = await _gdptRepository.GetTopicByIdAsync(updatedTopic.TopicID);

            return MapTopicToCurriculumDTO(fullUpdatedTopic!);
        }

    }
}

