using MathSlides.Business_Object.Models.DTOs.Auth;
using MathSlides.Service.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<LogoutResponse> LogoutAsync();
        Task<UserInfo> GetProfileAsync(ClaimsPrincipal user);
        Task<UserInfo> UpdateProfileAsync(int userIdFromToken, int userIdFromRoute, UpdateProfileRequestDTO request);
    }
}
