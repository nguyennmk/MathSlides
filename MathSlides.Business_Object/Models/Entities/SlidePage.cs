namespace MathSlides.Business_Object.Models.Entities
{
    public class SlidePage
    {
        public int PageID { get; set; }
        public int SlideID { get; set; }
        public int PageNumber { get; set; }
        public string? Title { get; set; }
        
        // Navigation properties
        public Slide Slide { get; set; } = null!;
        public ICollection<SlideElement> SlideElements { get; set; } = new List<SlideElement>();
    }
}

