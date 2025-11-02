namespace MathSlides.Business_Object.Models.Entities
{
    public class TopicVersion
    {
        public int VersionID { get; set; }
        public int TopicID { get; set; }
        public int VersionNumber { get; set; }
        public string? Changes { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Topic Topic { get; set; } = null!;
    }
}

