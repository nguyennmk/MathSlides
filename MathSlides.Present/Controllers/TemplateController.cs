using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Business_Object.Models.DTOs.Powerpoint;
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

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> ImportPptxTemplate([FromForm] PowerpointImportRequest request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                    return BadRequest(new { message = "File không được để trống" });

                // Gọi dịch vụ TEMPLATE (không phải Powerpoint)
                var newTemplate = await _templateService.ImportPptxAsync(request);

                // Trả về template đã được tạo
                return CreatedAtAction(nameof(GetTemplateById), new { id = newTemplate.TemplateID }, newTemplate);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi validation khi import PPTX.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import file PowerPoint: {FileName}", request.File?.FileName);
                return StatusCode(500, new { message = "Lỗi khi xử lý file PowerPoint", error = ex.Message });
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


        [HttpGet("download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadTemplate(int id)
        {
            try
            {
                var (filePath, contentType, fileName) = await _templateService.GetTemplateFileForDownloadAsync(id);

                return PhysicalFile(filePath, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải file template ID: {Id}", id);
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải file.", error = ex.Message });
            }
        }
    }
}

