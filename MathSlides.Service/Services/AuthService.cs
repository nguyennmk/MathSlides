using MathSlides.Business_Object.Models.DTOs.Auth;
using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace MathSlides.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
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
    }
}