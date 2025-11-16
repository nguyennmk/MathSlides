namespace MathSlides.Business_Object.Models.DTOs.Powerpoint
{
    public class PowerpointImportResponse
    {
        public string TemplatePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string JsonContent { get; set; } = string.Empty;
        public int SlideCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


