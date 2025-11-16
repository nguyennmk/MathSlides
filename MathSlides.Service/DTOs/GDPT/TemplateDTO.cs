namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class TemplateDTO
    {
        public int TemplateID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string? TemplateType { get; set; }
        public string? Tags { get; set; }
        public bool IsActive { get; set; }
    }

    public class TemplateDetailDTO : TemplateDTO
    {
        public ImportGDPTRequest? Content { get; set; }
    }
}

