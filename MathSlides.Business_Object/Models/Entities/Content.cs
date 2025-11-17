namespace MathSlides.Business_Object.Models.Entities
{
    public class Content
    {
        public int ContentID { get; set; }
        public int TopicID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }

        // Navigation properties\
        public string Source { get; set; } = string.Empty;
        public Topic Topic { get; set; } = null!;
        public ICollection<Formula> Formulas { get; set; } = new List<Formula>();
        public ICollection<Example> Examples { get; set; } = new List<Example>();
        public ICollection<Media> Media { get; set; } = new List<Media>();
    }
}

