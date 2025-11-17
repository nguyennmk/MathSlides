using MathSlides.Business_Object.Models.DTOs.GDPT;
using MathSlides.Service.DTOs.GDPT;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MathSlides.Present.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GDPTController : ControllerBase
    {
        private readonly IGDPTService _gdptService;
        private readonly ILogger<GDPTController> _logger;

        public GDPTController(IGDPTService gdptService, ILogger<GDPTController> logger)
        {
            _gdptService = gdptService;
            _logger = logger;
        }

        [HttpPost("import")]
        //[Authorize(Roles = "Admin,Teacher")] // Chỉ Admin và Teacher mới được import
        public async Task<IActionResult> ImportGDPTData([FromBody] ImportGDPTRequest request)
        {
            try
            {
                if (request == null || request.Topics == null || !request.Topics.Any())
                {
                    return BadRequest(new { message = "Request không hợp lệ. Vui lòng cung cấp dữ liệu topics." });
                }

                _logger.LogInformation($"Bắt đầu import {request.Topics.Count} topics");

                var response = await _gdptService.ImportGDPTDataAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import dữ liệu GDPT");
                return StatusCode(500, new ImportGDPTResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Import tài liệu GDPT từ file JSON
        /// </summary>
        [HttpPost("import-from-file")]
        //[Authorize(Roles = "Admin,Teacher")] // Chỉ Admin và Teacher mới được import
        public async Task<IActionResult> ImportGDPTFromFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn file JSON để import." });
                }

                if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "File phải có định dạng JSON." });
                }

                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<ImportGDPTRequest>(jsonContent, options);

                if (request == null || request.Topics == null || !request.Topics.Any())
                {
                    return BadRequest(new { message = "File JSON không hợp lệ hoặc không có dữ liệu." });
                }

                _logger.LogInformation($"Bắt đầu import từ file: {file.FileName}, {request.Topics.Count} topics");

                var response = await _gdptService.ImportGDPTDataAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(500, response);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi khi parse JSON");
                return BadRequest(new { message = $"Lỗi định dạng JSON: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import từ file");
                return StatusCode(500, new ImportGDPTResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        /// <summary>
        /// Lấy nội dung giáo trình theo Grade và Class
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurriculum(
                    [FromQuery(Name = "grade-name")] string gradeName,
                    [FromQuery(Name = "class-name")] string className,
                    [FromQuery(Name = "is-active")] bool? isActive)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gradeName) || string.IsNullOrWhiteSpace(className))
                {
                    return BadRequest(new { message = "grade-name và class-name là bắt buộc" });
                }

                // Truyền tham số mới vào service
                var curriculum = await _gdptService.GetCurriculumByGradeAndClassAsync(gradeName, className, isActive);

                if (curriculum == null || !curriculum.Any())
                {
                    return NotFound(new { message = $"Không tìm thấy giáo trình cho {gradeName} - {className}" });
                }

                return Ok(curriculum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy giáo trình");
                return StatusCode(500, new { message = "Lỗi khi lấy giáo trình", error = ex.Message });
            }
        }
        /// <summary>
        /// Cập nhật thông tin cơ bản của một Topic (Admin/Teacher)
        /// </summary>
        // PUT: api/curriculums/topics/5
        [HttpPut("topics/{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] UpdateTopicRequestDTO request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Topic ID không hợp lệ." });
            }

            try
            {
                var updatedTopic = await _gdptService.UpdateTopicAsync(id, request);
                return Ok(updatedTopic);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật topic {id}");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ.", details = ex.Message });
            }
        }
        [HttpDelete("topics/{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Topic ID không hợp lệ." });
            }

            try
            {
                var success = await _gdptService.SoftDeleteTopicAsync(id);

                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy Topic với ID: {id} (hoặc đã bị xóa)" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa mềm topic {id}");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ.", details = ex.Message });
            }
        }
    }
}

