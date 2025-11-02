namespace MathSlides.Business_Object.Models.Entities
{
    public class SlideElement
    {
        public int ElementID { get; set; }
        public int PageID { get; set; }
        public string Type { get; set; } = string.Empty; // 'Text', 'Formula', 'Image', 'Example'
        
        // Dùng cho Type = 'Text'
        public string? Content { get; set; }
        
        // Dùng để tham chiếu đến nội dung gốc
        public int? FormulaID { get; set; }
        public int? ExampleID { get; set; }
        public int? MediaID { get; set; }
        
        // Định vị, kích thước và thứ tự
        public int? PositionX { get; set; }
        public int? PositionY { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int ElementOrder { get; set; }
        
        // Navigation properties
        public SlidePage SlidePage { get; set; } = null!;
        public Formula? Formula { get; set; }
        public Example? Example { get; set; }
        public Media? Media { get; set; }
    }
}

