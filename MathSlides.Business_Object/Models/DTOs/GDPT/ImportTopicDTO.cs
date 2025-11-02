namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class ImportTopicDTO
    {
        public string TopicName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;
        public string StrandName { get; set; } = string.Empty;
        public string? Objectives { get; set; }
        public string? Source { get; set; }
        public List<ImportContentDTO> Contents { get; set; } = new List<ImportContentDTO>();
    }
}

