namespace MathSlides.Business_Object.Models.Entities
{
    public class Class
    {
        public int ClassID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GradeID { get; set; }
        
        // Navigation properties
        public Grade Grade { get; set; } = null!;
        public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }
}

