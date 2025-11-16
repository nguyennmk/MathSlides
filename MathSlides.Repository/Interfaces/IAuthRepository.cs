using MathSlides.Business_Object.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Repository.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<int> SaveChangesAsync();
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User> UpdateUserAsync(User user);
        Task<bool> SoftDeleteUserAsync(int userId);
        Task<List<Role>> GetAllRolesAsync();
    }
}
