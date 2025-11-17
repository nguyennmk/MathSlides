namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class CurriculumDTO
    {
        public int TopicID { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;
        public string StrandName { get; set; } = string.Empty;
        public string? Objectives { get; set; }
        public bool IsActive { get; set; }
        public string? Source { get; set; }
        public List<ContentDTO> Contents { get; set; } = new List<ContentDTO>();
    }

    public class ContentDTO
    {
        public int ContentID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public List<FormulaDTO> Formulas { get; set; } = new List<FormulaDTO>();
        public List<ExampleDTO> Examples { get; set; } = new List<ExampleDTO>();
        public List<MediaDTO> Media { get; set; } = new List<MediaDTO>();
    }

    public class FormulaDTO
    {
        public int FormulaID { get; set; }
        public string FormulaText { get; set; } = string.Empty;
        public string? Explanation { get; set; }
    }

    public class ExampleDTO
    {
        public int ExampleID { get; set; }
        public string ExampleText { get; set; } = string.Empty;
    }

    public class MediaDTO
    {
        public int MediaID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

