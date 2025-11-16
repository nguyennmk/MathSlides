using MathSlides.Business_Object.Models.Entities;
using MathSlides.Repository.Interfaces;
using MathSlides.Service.DTOs.Admin;
using MathSlides.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAuthRepository _authRepository;

        public AdminService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<List<AdminUserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _authRepository.GetAllUsersAsync();
            return users.Select(MapUserToDTO).ToList();
        }

        public async Task<AdminUserResponseDTO?> GetUserByIdAsync(int userId)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            return user != null ? MapUserToDTO(user) : null;
        }

        public async Task<List<AdminRoleResponseDTO>> GetAllRolesAsync()
        {
            var roles = await _authRepository.GetAllRolesAsync();
            return roles.Select(r => new AdminRoleResponseDTO
            {
                RoleID = r.RoleID,
                Name = r.Name
            }).ToList();
        }

        public async Task<AdminUserResponseDTO> UpdateUserAsync(int userId, AdminUpdateUserRequestDTO request)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            // Kiểm tra email/username có bị trùng với user khác không
            var existingEmail = await _authRepository.GetUserByEmailAsync(request.Email);
            if (existingEmail != null && existingEmail.UserID != userId)
            {
                throw new ArgumentException("Email already exists.");
            }

            var existingUsername = await _authRepository.GetUserByUsernameAsync(request.Username);
            if (existingUsername != null && existingUsername.UserID != userId)
            {
                throw new ArgumentException("Username already exists.");
            }

            // Cập nhật thông tin
            user.Username = request.Username;
            user.Email = request.Email;
            user.RoleID = request.RoleID;
            user.IsActive = request.IsActive;

            var updatedUser = await _authRepository.UpdateUserAsync(user);

            // Lấy lại Role Name nếu RoleID thay đổi
            var role = await _authRepository.GetRoleByIdAsync(updatedUser.RoleID);
            updatedUser.RoleName = role?.Name ?? string.Empty;

            return MapUserToDTO(updatedUser);
        }

        public async Task<bool> SoftDeleteUserAsync(int userId)
        {
            return await _authRepository.SoftDeleteUserAsync(userId);
        }

        // Hàm helper để map Entity sang DTO
        private AdminUserResponseDTO MapUserToDTO(User user)
        {
            return new AdminUserResponseDTO
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                RoleID = user.RoleID,
                RoleName = user.Role?.Name ?? user.RoleName, // Lấy từ navigation property nếu có
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
