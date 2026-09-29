using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.UserDtos;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract.Base
{
    public interface IUserService
    {
        Task<ApplicationUser> GetByIdAsync(int id);
        Task<IReadOnlyList<ApplicationUser>> GetAllAsync();
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
        Task UpdateUserAsync(ApplicationUser user);
        Task DeleteUserAsync(int id);
        Task<(string Token, ApplicationUser User)?> LoginWithUserAsync(string identifier, string password);
        Task<string> LoginAsync(string username, string password);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);
        Task<bool> AddUserToRoleAsync(int userId, string roleName);
        Task<bool> AddRoleAsync(string roleName);
        Task<IList<string>> GetUserRolesAsync(int userId);

        // Admin User Management Methods
        Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPagedUsersAsync(UserFilterDto filter);
        Task<ApplicationUser> UpdateUserAsync(int id, UpdateUserDto updateDto);
        Task<bool> BlockUserAsync(int id);
        Task<bool> UnblockUserAsync(int id);
        Task<UserStatisticsDto> GetUserStatisticsAsync();
        Task<bool> RemoveUserFromRoleAsync(int userId, string roleName);
        Task<bool> SetUserRolesAsync(int userId, List<string> roles);
    }
}
