namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class GradeDTO
    {
        public int GradeID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public List<ClassDTO> Classes { get; set; } = new List<ClassDTO>();
    }

    public class ClassDTO
    {
        public int ClassID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GradeID { get; set; }
        public string GradeName { get; set; } = string.Empty;
    }

    public class GradeClassStructureDTO
    {
        public List<GradeDTO> Grades { get; set; } = new List<GradeDTO>();
    }
}

