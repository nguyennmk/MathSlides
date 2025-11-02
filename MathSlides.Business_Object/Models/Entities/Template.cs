namespace MathSlides.Business_Object.Models.Entities
{
    public class Template
    {
        public int TemplateID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
        public string? TemplateType { get; set; }
        public string? Tags { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

