using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathSlides.Present.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateService _templateService;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(ITemplateService templateService, ILogger<TemplateController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả templates (không filter)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTemplates([FromQuery] bool onlyActive = false)
        {
            try
            {
                // Lấy tất cả templates không filter
                var templates = await _templateService.GetAllTemplatesAsync(onlyActive);
                
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách templates");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách templates", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy template chi tiết theo ID (bao gồm nội dung JSON)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    return NotFound(new { message = $"Template với ID {id} không tồn tại" });
                }

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy template {TemplateId}", id);
                return StatusCode(500, new { message = "Lỗi khi lấy template", error = ex.Message });
            }
        }
    }
}

