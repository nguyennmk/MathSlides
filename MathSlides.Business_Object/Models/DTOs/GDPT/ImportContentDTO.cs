namespace MathSlides.Business_Object.Models.DTOs.GDPT
{
    public class ImportContentDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public List<ImportFormulaDTO> Formulas { get; set; } = new List<ImportFormulaDTO>();
        public List<ImportExampleDTO> Examples { get; set; } = new List<ImportExampleDTO>();
        public List<ImportMediaDTO> Media { get; set; } = new List<ImportMediaDTO>();
    }
}

