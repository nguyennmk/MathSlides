namespace MathSlides.Business_Object.Models.Entities
{
    public class Strand
    {
        public int StrandID { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }
}

