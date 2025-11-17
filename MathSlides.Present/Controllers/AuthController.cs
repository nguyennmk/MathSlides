using MathSlides.Business_Object.Models.DTOs.Auth;
using MathSlides.Service.DTOs.Auth;
using MathSlides.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathSlides.Present.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var response = await _authService.LogoutAsync();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userInfo = await _authService.GetProfileAsync(User);
                return Ok(userInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the profile." });
            }
        }
        /// <summary>
        /// Người dùng tự cập nhật thông tin cá nhân (Username, Email, Password)
        /// </summary>
        // PUT: api/auth/users/5
        [HttpPut("users/{id}")]
        [Authorize] 
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequestDTO request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "User ID không hợp lệ." });
            }

            try
            {
                // Lấy User ID từ token JWT của người đang đăng nhập
                var userIdFromTokenString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdFromTokenString, out int userIdFromToken))
                {
                    return Unauthorized(new { message = "Token không hợp lệ." });
                }

                // Gọi service với cả 2 ID để check bảo mật
                var updatedUser = await _authService.UpdateProfileAsync(userIdFromToken, id, request);
                return Ok(updatedUser);
            }
            catch (UnauthorizedAccessException ex) // Lỗi 401/403: Cố sửa profile người khác
            {
                _logger.LogWarning(ex.Message);
                return Forbid(ex.Message); // 403 Forbidden
            }
            catch (KeyNotFoundException ex) // Lỗi 404: Không tìm thấy user
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex) // Lỗi 400: Trùng email/username
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật profile cho user {id}");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ." });
            }
        }
        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);
                return Ok(new { message = "Nếu email của bạn tồn tại trong hệ thống, chúng tôi đã gửi một mã khôi phục." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng ở API ForgotPassword");
                return Ok(new { message = "Nếu email của bạn tồn tại trong hệ thống, chúng tôi đã gửi một mã khôi phục." });
            }
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var success = await _authService.ResetPasswordAsync(request);
                if (!success)
                {
                    return BadRequest(new { message = "Mã khôi phục không hợp lệ hoặc đã hết hạn." });
                }

                return Ok(new { message = "Mật khẩu của bạn đã được thay đổi thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng ở API ResetPassword");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ." });
            }
        }
    }
}
