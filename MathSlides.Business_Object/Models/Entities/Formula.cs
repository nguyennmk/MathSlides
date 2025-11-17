namespace MathSlides.Business_Object.Models.Entities
{
    public class Formula
    {
        public int FormulaID { get; set; }
        public int ContentID { get; set; }
        public string FormulaText { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        
        public Content Content { get; set; } = null!;
        public ICollection<SlideElement> SlideElements { get; set; } = new List<SlideElement>();
    }
}

