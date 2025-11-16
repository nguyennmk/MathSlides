using MathSlides.Service.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminUserResponseDTO>> GetAllUsersAsync();
        Task<AdminUserResponseDTO?> GetUserByIdAsync(int userId);
        Task<AdminUserResponseDTO> UpdateUserAsync(int userId, AdminUpdateUserRequestDTO request);
        Task<bool> SoftDeleteUserAsync(int userId);
        Task<List<AdminRoleResponseDTO>> GetAllRolesAsync();
    }
}
