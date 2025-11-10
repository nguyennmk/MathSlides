using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    /// <summary>
    /// Interface chính điều phối toàn bộ nghiệp vụ tạo slide
    /// </summary>
    public interface ISlideGenerationService
    {
        /// <summary>
        /// Luồng nghiệp vụ chính: 
        /// 1. Gọi Gemini để tạo nội dung cho Topic.
        /// 2. Lưu nội dung vào DB (bảng Content, Formula, Example).
        /// 3. Đọc JSON template.
        /// 4. Tạo file PPTX và trả về.
        /// </summary>
        Task<(MemoryStream stream, string fileName)> GenerateSlidesFromTopicAsync(int topicId, string templateName);
    }
}
