using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.DTOs.Admin
{
    public class AdminUserResponseDTO
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO để Admin cập nhật User
    public class AdminUpdateUserRequestDTO
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int RoleID { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }

    // DTO để hiển thị Role
    public class AdminRoleResponseDTO
    {
        public int RoleID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
