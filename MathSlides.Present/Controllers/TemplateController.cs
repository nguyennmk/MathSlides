using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Service.DTOs.Admin;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathSlides.Present.Controllers
{
    [Route("api/template")]
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

        // POST: api/templates
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateTemplate([FromForm] CreateTemplateRequestDTO request)
        {
            // Model binding sẽ tự động validate [Required]

            try
            {
                var newTemplate = await _templateService.CreateTemplateAsync(request);
                return CreatedAtAction(nameof(GetTemplateById), new { id = newTemplate.TemplateID }, newTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo template");
                return StatusCode(500, new { message = "Lỗi khi tạo template", error = ex.Message });
            }
        }

        // PUT: api/templates/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromForm] UpdateTemplateRequestDTO request)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid Template ID.");
            }

            try
            {
                var updatedTemplate = await _templateService.UpdateTemplateAsync(id, request);
                return Ok(updatedTemplate);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật template {id}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật template", error = ex.Message });
            }
        }

        // DELETE: api/templates/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid Template ID.");
            }

            try
            {
                var success = await _templateService.DeleteTemplateAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Template with ID {id} not found." });
                }
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa template {id}");
                return StatusCode(500, new { message = "Lỗi khi xóa template", error = ex.Message });
            }
        }
    }
}

