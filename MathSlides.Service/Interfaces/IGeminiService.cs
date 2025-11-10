using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    /// <summary>
    /// Interface cho dịch vụ gọi Google Gemini API
    /// (Đây là file bạn yêu cầu)
    /// </summary>
    public interface IGeminiService
    {
        /// <summary>
        /// Gửi một prompt (câu lệnh) đến Gemini và nhận về một chuỗi nội dung.
        /// Chuỗi này dự kiến là một JSON.
        /// </summary>
        Task<string> GenerateContentAsync(string prompt);
    }
}
