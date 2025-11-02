namespace MathSlides.Business_Object.Models.Entities
{
    public class Example
    {
        public int ExampleID { get; set; }
        public int ContentID { get; set; }
        public string ExampleText { get; set; } = string.Empty;
        
        // Navigation properties
        public Content Content { get; set; } = null!;
        public ICollection<SlideElement> SlideElements { get; set; } = new List<SlideElement>();
    }
}

