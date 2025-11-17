using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.DTOs.Generation
{
    public class GenerationRequest
    {
        public int TopicId { get; set; }       
        public string TemplateName { get; set; } = "default_math_template.json";           
        public string? Name { get; set; }
        public string? Objectives { get; set; }
        public string? Source { get; set; }
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
        public string? ExampleText { get; set; }
        public string? FormulaText { get; set; }
        public string? Explanation { get; set; }
    }
}
