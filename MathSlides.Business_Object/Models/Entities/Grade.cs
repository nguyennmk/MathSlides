namespace MathSlides.Business_Object.Models.Entities
{
    public class Grade
    {
        public int GradeID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        
        public ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}

