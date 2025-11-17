namespace MathSlides.Business_Object.Models.Entities
{
    public class Media
    {
        public int MediaID { get; set; }
        public int ContentID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public Content Content { get; set; } = null!;
        public ICollection<SlideElement> SlideElements { get; set; } = new List<SlideElement>();
    }
}

