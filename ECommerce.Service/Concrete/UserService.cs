using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.UserDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;

        public UserService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // Kullanıcı oluşturma (register)
        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
        {
            user.Email = user.Email?.Trim().ToLowerInvariant();
            user.UserName = user.Email;
            user.PhoneNumber = NormalizePhone(user.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var existingPhones = await _userManager.Users
                    .Where(candidate => candidate.PhoneNumber != null)
                    .Select(candidate => candidate.PhoneNumber!)
                    .ToListAsync();
                if (existingPhones.Any(phone => NormalizePhone(phone) == user.PhoneNumber))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DuplicatePhoneNumber",
                        Description = "Bu telefon numarası başka bir hesapta kullanılıyor."
                    });
                }
            }

            user.LockoutEnabled = true;
            var result = await _userManager.CreateAsync(user, password);
            return result;
        }

        public async Task<(string Token, ApplicationUser User)?> LoginWithUserAsync(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            ApplicationUser? user = null;

            if (identifier.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(identifier);
            }

            if (user == null)
            {
                var normalizedPhone = NormalizePhone(identifier);
                if (!string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    var phoneUsers = await _userManager.Users
                        .Where(candidate => candidate.PhoneNumber != null)
                        .ToListAsync();
                    user = phoneUsers.FirstOrDefault(candidate => NormalizePhone(candidate.PhoneNumber) == normalizedPhone);
                }
            }

            // Admin panelindeki mevcut hesaplar kullanıcı adıyla oluşturulmuş olabilir.
            // Müşteri arayüzü e-posta/telefon istemeye devam ederken eski yönetici
            // hesaplarının kullanıcı adıyla girişini geriye uyumlu tutar.
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(identifier);
            }

            if (user == null)
                return null;

            if (!user.IsActive || await _userManager.IsLockedOutAsync(user))
                return null;

            if (!user.LockoutEnabled)
            {
                var lockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
                if (!lockoutResult.Succeeded) return null;
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);
                return null;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var token = await GenerateJwtTokenAsync(user);
            return (token, user);
        }

        private static string? NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0090")) digits = digits[4..];
            else if (digits.StartsWith("90") && digits.Length > 10) digits = digits[2..];
            if (digits.StartsWith('0') && digits.Length > 10) digits = digits[1..];
            return digits;
        }

        // Login işlemi (şifre doğrulama ve JWT token oluşturma)
        public async Task<string> LoginAsync(string username, string password)
        {
            var result = await LoginWithUserAsync(username, password);
            return result?.Token;
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _unitOfWork.UserRepository.GetByEmailAsync(email);
        }

        public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _unitOfWork.UserRepository.GetByNameAsync(username);
        }

        // JWT Token üretme
        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Rol ekleme
        public async Task<bool> AddRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName };
                var result = await _roleManager.CreateAsync(role);
                return result.Succeeded;
            }
            return false;
        }

        // Kullanıcıya rol atama
        public async Task<bool> AddUserToRoleAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (!await _roleManager.RoleExistsAsync(roleName))
                return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        // Kullanıcının rollerini alma
        public async Task<IList<string>> GetUserRolesAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles;
        }

        // Diğer CRUD işlemleri (Delete, GetAll, GetById, Update) - değişmeden kalabilir
        public async Task DeleteUserAsync(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (user != null)
            {
                await _unitOfWork.UserRepository.DeleteAsync(user);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetAllAsync()
        {
            return await _unitOfWork.UserRepository.GetAllAsync();
        }

        public async Task<ApplicationUser> GetByIdAsync(int id)
        {
            return await _unitOfWork.UserRepository.GetByIdAsync(id);
        }

        public async Task UpdateUserAsync(ApplicationUser user)
        {
            await _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();
        }

        // Admin User Management Methods Implementation
        public async Task<(IReadOnlyList<ApplicationUser> Users, int TotalCount)> GetPagedUsersAsync(UserFilterDto filter)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.UserName.ToLower().Contains(searchTerm));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (filter.EmailConfirmed.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == filter.EmailConfirmed.Value);
            }

            if (filter.CreatedAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= filter.CreatedAfter.Value);
            }

            if (filter.CreatedBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= filter.CreatedBefore.Value);
            }

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "email" => filter.SortOrder.ToLower() == "desc" 
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "firstname" => filter.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.FirstName)
                    : query.OrderBy(u => u.FirstName),
                "lastname" => filter.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.LastName)
                    : query.OrderBy(u => u.LastName),
                _ => filter.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt)
            };

            var totalCount = query.Count();
            var users = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            // Filter by role if specified (requires separate check due to Identity framework)
            if (!string.IsNullOrEmpty(filter.Role))
            {
                var usersInRole = new List<ApplicationUser>();
                foreach (var user in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains(filter.Role))
                    {
                        usersInRole.Add(user);
                    }
                }
                users = usersInRole;
            }

            return (users.AsReadOnly(), totalCount);
        }

        public async Task<ApplicationUser> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return null;

            // Update user properties
            user.Email = updateDto.Email;
            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.IsActive = updateDto.IsActive;
            user.EmailConfirmed = updateDto.EmailConfirmed;
            user.PhoneNumberConfirmed = updateDto.PhoneNumberConfirmed;

            if (!string.IsNullOrEmpty(updateDto.UserName))
                user.UserName = updateDto.UserName;

            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"User update failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            // Update roles if provided
            if (updateDto.Roles != null && updateDto.Roles.Any())
            {
                await SetUserRolesAsync(id, updateDto.Roles);
            }

            return user;
        }

        public async Task<bool> BlockUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return false;

            user.IsActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue; // Permanent lockout
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UnblockUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return false;

            user.IsActive = true;
            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync()
        {
            var allUsers = _userManager.Users.ToList();
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var statistics = new UserStatisticsDto
            {
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                InactiveUsers = allUsers.Count(u => !u.IsActive),
                BlockedUsers = allUsers.Count(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow),
                EmailConfirmedUsers = allUsers.Count(u => u.EmailConfirmed),
                EmailUnconfirmedUsers = allUsers.Count(u => !u.EmailConfirmed),
                UsersRegisteredToday = allUsers.Count(u => u.CreatedAt.Date == today),
                UsersRegisteredThisWeek = allUsers.Count(u => u.CreatedAt >= weekStart),
                UsersRegisteredThisMonth = allUsers.Count(u => u.CreatedAt >= monthStart),
            };

            if (allUsers.Any())
            {
                statistics.LastRegistrationDate = allUsers.Max(u => u.CreatedAt);
                var totalDays = (DateTime.UtcNow - allUsers.Min(u => u.CreatedAt)).TotalDays;
                statistics.AverageUsersPerDay = totalDays > 0 ? allUsers.Count / totalDays : 0;
            }

            // Get users by role
            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                statistics.UsersByRole[role.Name] = usersInRole.Count;
            }

            return statistics;
        }

        public async Task<bool> RemoveUserFromRoleAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (!await _roleManager.RoleExistsAsync(roleName))
                return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<bool> SetUserRolesAsync(int userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove all current roles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return false;
            }

            // Add new roles
            if (roles.Any())
            {
                // Verify all roles exist
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                        return false;
                }

                var addResult = await _userManager.AddToRolesAsync(user, roles);
                return addResult.Succeeded;
            }

            return true;
        }
    }
}
