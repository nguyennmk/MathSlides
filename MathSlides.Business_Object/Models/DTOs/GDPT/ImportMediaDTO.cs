namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class ImportMediaDTO
    {
        public string Type { get; set; } = "Image";
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

