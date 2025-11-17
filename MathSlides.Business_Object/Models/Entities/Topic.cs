namespace MathSlides.Business_Object.Models.Entities
{
    public class Topic
    {
        public int TopicID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public int StrandID { get; set; }
        public string? Objectives { get; set; }
        public string? Source { get; set; }
        
        // Navigation properties
        public Class Class { get; set; } = null!;
        public Strand Strand { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public ICollection<Content> Contents { get; set; } = new List<Content>();
        public ICollection<TopicVersion> TopicVersions { get; set; } = new List<TopicVersion>();
        public ICollection<Slide> Slides { get; set; } = new List<Slide>();
    }
}

