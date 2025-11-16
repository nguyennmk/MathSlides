namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class ImportGDPTResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalTopicsImported { get; set; }
        public int TotalContentsImported { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}

