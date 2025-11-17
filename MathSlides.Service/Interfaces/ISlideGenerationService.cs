using MathSlides.Service.DTOs.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface ISlideGenerationService
    {
        Task<(MemoryStream stream, string fileName)> GenerateSlidesFromTopicAsync(int topicId, string templateName);
        Task<(MemoryStream stream, string fileName)> GenerateSlidesFromPptxTemplateAsync(GenerationRequest request);
    }
}
