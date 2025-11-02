namespace MathSlides.Business_Object.Models.Entities
{
    public class Slide
    {
        public int SlideID { get; set; }
        public int UserID { get; set; }
        public int TopicID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public Topic Topic { get; set; } = null!;
        public ICollection<SlidePage> SlidePages { get; set; } = new List<SlidePage>();
    }
}

