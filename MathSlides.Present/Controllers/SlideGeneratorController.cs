using MathSlides.Service.DTOs.Generation;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using System;                      
using System.IO;                   
using System.Text.Json;            
using System.Threading.Tasks;      

namespace MathSlides.Present.Controllers
{
    [Route("api/generation")]
    public class SlideGeneratorController : ControllerBase
    {
        private readonly ISlideGenerationService _slideGenerationService;
        private readonly ILogger<SlideGeneratorController> _logger;

        public SlideGeneratorController(ISlideGenerationService slideGenerationService, ILogger<SlideGeneratorController> logger)
        {
            _slideGenerationService = slideGenerationService;
            _logger = logger;
        }

        [HttpPost("generate-pptx")]
        [ProducesResponseType(typeof(FileContentResult), 200)] 
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
                var (stream, fileName) = await _slideGenerationService.GenerateSlidesFromTopicAsync(request.TopicId, request.TemplateName);

                if (stream == null || stream.Length == 0)
                {
                    return NotFound(new { message = "Không thể tạo PPTX." });
                }
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

        [HttpPost("generate-from-pptx-template")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateSlidesFromPptxTemplate([FromBody] GenerationRequest request)
        {
            // 1. Validation (Đã chính xác)
            if (request.TopicId <= 0 || string.IsNullOrWhiteSpace(request.TemplateName))
            {
                return BadRequest(new { message = "TopicId và TemplateName là bắt buộc." });
            }

            if (!request.TemplateName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "TemplateName cho luồng này phải là một file .pptx." });
            }

            try
            {
                // 2. Gọi Service với DTO (Đã chính xác)
                var (stream, fileName) = await _slideGenerationService.GenerateSlidesFromPptxTemplateAsync(request);

                if (stream == null || stream.Length == 0)
                {
                    return NotFound(new { message = "Không thể tạo PPTX." });
                }

                string mimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

                // 3. Trả về file
                // PHẦN QUAN TRỌNG: Hãy đảm bảo service của bạn đã gọi stream.Position = 0;
                // Nhưng để chắc chắn, Controller nên gọi .ToArray()
                return File(stream.ToArray(), mimeType, fileName);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Lỗi file không tìm thấy khi tạo slide");
                return NotFound(new { message = ex.Message });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Lỗi parse JSON (từ Gemini) khi tạo slide");
                return BadRequest(new { message = "Lỗi xử lý JSON (từ Gemini): " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi tạo slide");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ.", details = ex.Message });
            }
        }
    }

    
}