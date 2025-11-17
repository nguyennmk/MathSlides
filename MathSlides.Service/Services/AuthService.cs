using BCrypt.Net;
using MathSlides.Business_Object.Models.DTOs.Auth;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.DTOs.Auth;
using MathSlides.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, ILogger<AuthService> logger, IEmailService emailService)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Validate
            if (await _authRepository.UsernameExistsAsync(request.Username))
                throw new ArgumentException("Username already exists");

            if (await _authRepository.EmailExistsAsync(request.Email))
                throw new ArgumentException("Email already exists");

            var role = await _authRepository.GetRoleByIdAsync(request.RoleID);
            if (role == null)
                throw new ArgumentException("Invalid role");

            // Hash password - Sửa lỗi BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleID = request.RoleID,
                CreatedAt = DateTime.UtcNow,
                RoleName = role.Name
            };

            await _authRepository.CreateUserAsync(user);

            return await GenerateTokenAsync(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid Email or password");

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid Email or password");
            }
            // Verify password - Sửa lỗi BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid Email or password");

            // Update RoleName if null
            if (string.IsNullOrEmpty(user.RoleName))
            {
                var role = await _authRepository.GetRoleByIdAsync(user.RoleID);
                user.RoleName = role?.Name ?? string.Empty;
            }

            return await GenerateTokenAsync(user);
        }

        public async Task<LogoutResponse> LogoutAsync()
        {
            return new LogoutResponse
            {
                Success = true,
                Message = "Logged out successfully. Please clear token from client."
            };
        }

        private async Task<AuthResponse> GenerateTokenAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Sửa lỗi ToUnixTimeSeconds - sử dụng DateTimeOffset
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                         ClaimValueTypes.Integer64)
            };

            // Lấy JWT config an toàn hơn
            var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not found");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MathSlidesAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "MathSlidesClient";
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds);

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                User = new UserInfo
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.RoleName
                }
            };
        }
        public Task<UserInfo> GetProfileAsync(ClaimsPrincipal user)
        {
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = user.FindFirst(ClaimTypes.Name)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid token: UserID is missing or invalid.");
            }

            var userInfo = new UserInfo
            {
                UserID = userId,
                Username = username ?? string.Empty,
                Email = email ?? string.Empty,
                Role = role ?? string.Empty
            };

            return Task.FromResult(userInfo);
        }
        public async Task<UserInfo> UpdateProfileAsync(int userIdFromToken, int userIdFromRoute, UpdateProfileRequestDTO request)
        {
            // User ID trong token (người đang đăng nhập) phải khớp với User ID trên route (người bị sửa)
            if (userIdFromToken != userIdFromRoute)
            {
                _logger.LogWarning($"Security violation: User {userIdFromToken} attempted to update profile of user {userIdFromRoute}.");
                throw new UnauthorizedAccessException("Bạn chỉ có thể cập nhật thông tin của chính mình.");
            }

            var user = await _authRepository.GetUserByIdAsync(userIdFromToken);
            if (user == null || !user.IsActive)
            {
                throw new KeyNotFoundException("Không tìm thấy tài khoản hoặc tài khoản đã bị khóa.");
            }

            // 3. Kiểm tra Username/Email trùng lặp (với user khác)
            if (user.Email != request.Email && await _authRepository.EmailExistsAsync(request.Email))
            {
                throw new ArgumentException("Email này đã được sử dụng bởi tài khoản khác.");
            }
            if (user.Username != request.Username && await _authRepository.UsernameExistsAsync(request.Username))
            {
                throw new ArgumentException("Username này đã được sử dụng bởi tài khoản khác.");
            }

            user.Username = request.Username;
            user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            var updatedUser = await _authRepository.UpdateUserAsync(user);

            return new UserInfo
            {
                UserID = updatedUser.UserID,
                Username = updatedUser.Username,
                Email = updatedUser.Email,
                Role = updatedUser.Role?.Name ?? updatedUser.RoleName
            };
        }
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning($"Yêu cầu ForgotPassword cho email không tồn tại hoặc bị khóa: {request.Email}");
                return true;
            }

            var code = GenerateComplexRandomToken(16);

            user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(code);

            user.PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(5);

            await _authRepository.UpdateUserAsync(user);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, code);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email reset password cho {user.Email}");
                return true;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning($"ResetPassword thất bại: User không tồn tại hoặc bị khóa ({request.Email})");
                return false;
            }

            if (string.IsNullOrEmpty(user.PasswordResetToken) ||
                user.PasswordResetTokenExpires == null ||
                user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                _logger.LogWarning($"ResetPassword thất bại: Token hết hạn hoặc không tồn tại cho ({request.Email})");
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Token, user.PasswordResetToken))
            {
                _logger.LogWarning($"ResetPassword thất bại: Token không hợp lệ cho ({request.Email})");
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _authRepository.UpdateUserAsync(user);
            _logger.LogInformation($"ResetPassword thành công cho {request.Email}");
            return true;
        }

        private string GenerateComplexRandomToken(int length)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}";

            var charSet = new StringBuilder();
            charSet.Append(lowercase);
            charSet.Append(uppercase);
            charSet.Append(numbers);
            charSet.Append(special);

            var result = new StringBuilder();

            result.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            result.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            result.Append(numbers[RandomNumberGenerator.GetInt32(numbers.Length)]);
            result.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            string allChars = charSet.ToString();
            for (int i = 0; i < length - 4; i++)
            {
                result.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }
            return new string(result.ToString().ToCharArray().OrderBy(c => RandomNumberGenerator.GetInt32(0, int.MaxValue)).ToArray());
        }
    }
}