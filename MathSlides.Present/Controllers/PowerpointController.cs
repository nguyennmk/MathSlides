using MathSlides.Business_Object.Models.DTOs.Powerpoint;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathSlides.Present.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PowerpointController : ControllerBase
    {
        private readonly IPowerpointService _powerpointService;
        private readonly ILogger<PowerpointController> _logger;

        public PowerpointController(IPowerpointService powerpointService, ILogger<PowerpointController> logger)
        {
            _powerpointService = powerpointService;
            _logger = logger;
        }

        /// <summary>
        /// Import file PowerPoint và chuyển đổi sang JSON
        /// </summary>
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [AllowAnonymous]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
        public async Task<IActionResult> ImportPowerpoint([FromForm] PowerpointImportRequest request)
        {
            try
            {
                var file = request.File;

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "File không được để trống" });

                var allowedExtensions = new[] { ".pptx", ".ppt" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = "Chỉ chấp nhận file PowerPoint (.pptx, .ppt)" });

                using var stream = file.OpenReadStream();
                var result = await _powerpointService.ImportPowerpointAsync(
                    stream, file.FileName, request.Name, request.Description);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        templatePath = result.TemplatePath,
                        fileName = result.FileName,
                        slideCount = result.SlideCount,
                        jsonContent = result.JsonContent
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import PowerPoint file: {FileName}", request.File?.FileName);
                return StatusCode(500, new { message = "Lỗi khi xử lý file PowerPoint", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin PowerPoint từ file JSON đã import
        /// </summary>
        [HttpGet("info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPowerpointInfo([FromQuery] string templatePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    return BadRequest(new { message = "templatePath không được để trống" });
                }

                var result = await _powerpointService.GetPowerpointInfoAsync(templatePath);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        templatePath = result.TemplatePath,
                        fileName = result.FileName,
                        slideCount = result.SlideCount,
                        jsonContent = result.JsonContent
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "File không tồn tại: {TemplatePath}", templatePath);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin PowerPoint: {TemplatePath}", templatePath);
                return StatusCode(500, new { message = "Lỗi khi đọc file PowerPoint", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật nội dung JSON của file PowerPoint đã import
        /// </summary>
        [HttpPut("update")]
        [Consumes("application/json")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdatePowerpointInfo([FromBody] PowerpointUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.TemplatePath))
                {
                    return BadRequest(new { message = "templatePath không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.JsonContent))
                {
                    return BadRequest(new { message = "jsonContent không được để trống" });
                }

                var result = await _powerpointService.UpdatePowerpointInfoAsync(request.TemplatePath, request.JsonContent);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        templatePath = result.TemplatePath,
                        fileName = result.FileName,
                        slideCount = result.SlideCount,
                        jsonContent = result.JsonContent
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "File không tồn tại: {TemplatePath}", request?.TemplatePath);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "JSON content không hợp lệ: {TemplatePath}", request?.TemplatePath);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật file PowerPoint: {TemplatePath}", request?.TemplatePath);
                return StatusCode(500, new { message = "Lỗi khi cập nhật file PowerPoint", error = ex.Message });
            }
        }

        /// <summary>
        /// Link TemplatePath với Template ID
        /// </summary>
        [HttpPost("link-template")]
        [Consumes("application/json")]
        [AllowAnonymous]
        public async Task<IActionResult> LinkTemplatePath([FromBody] LinkTemplatePathRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request không được để trống" });
                }

                if (request.TemplateID <= 0)
                {
                    return BadRequest(new { message = "TemplateID phải lớn hơn 0" });
                }

                if (string.IsNullOrWhiteSpace(request.TemplatePath))
                {
                    return BadRequest(new { message = "TemplatePath không được để trống" });
                }

                var templatePath = await _powerpointService.SaveTemplatePathAsync(request.TemplatePath, request.TemplateID);

                return Ok(new
                {
                    success = true,
                    message = "Đã cập nhật TemplatePath thành công",
                    data = new
                    {
                        templateId = request.TemplateID,
                        templatePath = templatePath
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Template không tồn tại: {TemplateID}", request?.TemplateID);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi link TemplatePath với Template ID: {TemplateID}", request?.TemplateID);
                return StatusCode(500, new { message = "Lỗi khi cập nhật TemplatePath", error = ex.Message });
            }
        }
    }
}


