using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // <-- Cho ILogger
using System;                       // <-- Cho Exception
using System.IO;                    // <-- Cho FileNotFoundException
using System.Text.Json;             // <-- Cho JsonException
using System.Threading.Tasks;       // <-- Cho Task

namespace MathSlides.Present.Controllers
{
    // === KHÔNG CÓ [ApiController] ===
    [Route("api/generation")]
    public class SlideGeneratorController : ControllerBase
    {
        private readonly ISlideGenerationService _slideGenerationService;
        private readonly ILogger<SlideGeneratorController> _logger;

        // Constructor (Đã đúng)
        public SlideGeneratorController(ISlideGenerationService slideGenerationService, ILogger<SlideGeneratorController> logger)
        {
            _slideGenerationService = slideGenerationService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo và xuất file PPTX từ một Topic ID và Tên Template
        /// (Đây chính là luồng nghiệp vụ "Lựa chọn 2")
        /// </summary>
        [HttpPost("generate-pptx")]
        [ProducesResponseType(typeof(FileContentResult), 200)] // <-- Thêm 'typeof'
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateSlides([FromBody] GenerationRequest request)
        {
            if (request.TopicId <= 0 || string.IsNullOrWhiteSpace(request.TemplateName))
            {
                return BadRequest(new { message = "TopicId và TemplateName là bắt buộc." });
            }

            try
            {
                // Gọi "Nhạc trưởng"
                var (stream, fileName) = await _slideGenerationService.GenerateSlidesFromTopicAsync(request.TopicId, request.TemplateName);

                if (stream == null || stream.Length == 0)
                {
                    return NotFound(new { message = "Không thể tạo PPTX." });
                }

                // Trả file về
                string mimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                return File(stream.ToArray(), mimeType, fileName);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Lỗi file không tìm thấy khi tạo slide");
                return NotFound(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Lỗi parse JSON khi tạo slide");
                return BadRequest(new { message = "Lỗi xử lý JSON (từ Gemini hoặc Template): " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi tạo slide");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ.", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// DTO cho yêu cầu tạo slide
    /// </summary>
    public class GenerationRequest
    {
        public int TopicId { get; set; }
        // Đặt tên template mặc định
        public string TemplateName { get; set; } = "default_math_template.json";
    }
}