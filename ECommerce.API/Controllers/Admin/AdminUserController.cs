using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.UserDtos;
using ECommerce.Service.Response;
using ECommerce.Service.Mapping.Manual;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace ECommerce.API.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminUserController> logger)
        {
            _userService = userService;
            _userManager = userManager;
            _logger = logger;
        }

        public class ChangeOwnPasswordDto
        {
            [Required]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 8)]
            public string NewPassword { get; set; } = string.Empty;
        }

        public class ChangeOwnEmailDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string CurrentPassword { get; set; } = string.Empty;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var id)
                ? await _userManager.FindByIdAsync(id.ToString())
                : null;
        }

        /// <summary>Giriş yapan yöneticinin profil bilgisini getirir.</summary>
        [HttpGet("me")]
        public async Task<ActionResult<DataResponse<UserDto>>> GetOwnProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(BaseResponse.CreateFailure("Oturum sahibi bulunamadı."));

            var userDto = user.ToDto();
            userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return Ok(DataResponse<UserDto>.CreateSuccess(userDto));
        }

        /// <summary>Giriş yapan yöneticinin kendi şifresini değiştirir.</summary>
        [HttpPut("me/password")]
        public async Task<ActionResult<BaseResponse>> ChangeOwnPassword([FromBody] ChangeOwnPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse.CreateFailure("Geçersiz veri."));

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(BaseResponse.CreateFailure("Oturum sahibi bulunamadı."));

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(BaseResponse.CreateFailure(string.Join(" ", result.Errors.Select(error => error.Description))));

            return Ok(BaseResponse.CreateSuccess("Şifreniz başarıyla değiştirildi."));
        }

        /// <summary>Giriş yapan yöneticinin kendi e-posta adresini değiştirir.</summary>
        [HttpPut("me/email")]
        public async Task<ActionResult<BaseResponse>> ChangeOwnEmail([FromBody] ChangeOwnEmailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse.CreateFailure("Geçersiz e-posta adresi."));

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(BaseResponse.CreateFailure("Oturum sahibi bulunamadı."));

            if (!await _userManager.CheckPasswordAsync(user, dto.CurrentPassword))
                return BadRequest(BaseResponse.CreateFailure("Mevcut şifreniz doğru değil."));

            var result = await _userManager.SetEmailAsync(user, dto.Email.Trim());
            if (!result.Succeeded)
                return BadRequest(BaseResponse.CreateFailure(string.Join(" ", result.Errors.Select(error => error.Description))));

            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);
            return Ok(BaseResponse.CreateSuccess("E-posta adresiniz değiştirildi. E-posta onayı yeniden gereklidir."));
        }

        /// <summary>
        /// Kullanıcıları sayfalama ve filtreleme ile getirir
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<UserDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers([FromQuery] UserFilterDto filter)
        {
            try
            {
                var (users, totalCount) = await _userService.GetPagedUsersAsync(filter);
                var userDtos = users.Select(user => user.ToDto()).ToList();

                // Get roles for each user
                foreach (var userDto in userDtos)
                {
                    var roles = await _userService.GetUserRolesAsync(userDto.Id);
                    userDto.Roles = roles?.ToList() ?? new List<string>();
                }

                var response = new PagedResponse<UserDto>
                {
                    Success = true,
                    Message = "Kullanıcılar başarıyla getirildi",
                    Items = userDtos,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcılar getirilemedi"));
            }
        }

        /// <summary>
        /// Belirli bir kullanıcıyı getirir
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DataResponse<UserDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<UserDto>>> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                var userDto = user.ToDto();
                var roles = await _userService.GetUserRolesAsync(id);
                userDto.Roles = roles?.ToList() ?? new List<string>();

                return Ok(DataResponse<UserDto>.CreateSuccess(userDto, "Kullanıcı başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} getirilirken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı getirilemedi"));
            }
        }

        /// <summary>
        /// Kullanıcı bilgilerini günceller
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DataResponse<UserDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<DataResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(BaseResponse.CreateFailure("Geçersiz veri"));

                var updatedUser = await _userService.UpdateUserAsync(id, dto);
                if (updatedUser == null)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                var userDto = updatedUser.ToDto();
                var roles = await _userService.GetUserRolesAsync(id);
                userDto.Roles = roles?.ToList() ?? new List<string>();

                return Ok(DataResponse<UserDto>.CreateSuccess(userDto, "Kullanıcı başarıyla güncellendi"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Kullanıcı {UserId} güncellenirken hata oluştu", id);
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} güncellenirken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı güncellenemedi"));
            }
        }

        /// <summary>
        /// Kullanıcıyı siler
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> DeleteUser(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                await _userService.DeleteUserAsync(id);
                return Ok(BaseResponse.CreateSuccess("Kullanıcı başarıyla silindi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} silinirken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı silinemedi"));
            }
        }

        /// <summary>
        /// Kullanıcıyı engeller
        /// </summary>
        [HttpPost("{id}/block")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> BlockUser(int id)
        {
            try
            {
                var success = await _userService.BlockUserAsync(id);
                if (!success)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                return Ok(BaseResponse.CreateSuccess("Kullanıcı başarıyla engellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} engellenirken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı engellenemedi"));
            }
        }

        /// <summary>
        /// Kullanıcının engelini kaldırır
        /// </summary>
        [HttpPost("{id}/unblock")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> UnblockUser(int id)
        {
            try
            {
                var success = await _userService.UnblockUserAsync(id);
                if (!success)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                return Ok(BaseResponse.CreateSuccess("Kullanıcının engeli başarıyla kaldırıldı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} engeli kaldırılırken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı engeli kaldırılamadı"));
            }
        }

        /// <summary>
        /// Kullanıcı istatistiklerini getirir
        /// </summary>
        [HttpGet("statistics")]
        [ResponseCache(Duration = 300)] // 5 dakika cache
        [ProducesResponseType(typeof(DataResponse<UserStatisticsDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<UserStatisticsDto>>> GetUserStatistics()
        {
            try
            {
                var statistics = await _userService.GetUserStatisticsAsync();
                return Ok(DataResponse<UserStatisticsDto>.CreateSuccess(statistics, "Kullanıcı istatistikleri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı istatistikleri getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("İstatistikler getirilemedi"));
            }
        }

        /// <summary>
        /// Kullanıcıya rol atar
        /// </summary>
        [HttpPost("{id}/roles")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<BaseResponse>> AddUserToRole(int id, [FromBody] RoleRequestModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.RoleName))
                    return BadRequest(BaseResponse.CreateFailure("Rol adı gereklidir"));

                var success = await _userService.AddUserToRoleAsync(id, model.RoleName);
                if (!success)
                    return BadRequest(BaseResponse.CreateFailure("Kullanıcıya rol atama başarısız"));

                return Ok(BaseResponse.CreateSuccess("Rol başarıyla atandı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} rol atanırken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Rol atanamadı"));
            }
        }

        /// <summary>
        /// Kullanıcıdan rol kaldırır
        /// </summary>
        [HttpDelete("{id}/roles/{roleName}")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> RemoveUserFromRole(int id, string roleName)
        {
            try
            {
                var success = await _userService.RemoveUserFromRoleAsync(id, roleName);
                if (!success)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı veya rol bulunamadı"));

                return Ok(BaseResponse.CreateSuccess("Rol başarıyla kaldırıldı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} rolü {RoleName} kaldırılırken hata oluştu", id, roleName);
                return StatusCode(500, BaseResponse.CreateFailure("Rol kaldırılamadı"));
            }
        }

        /// <summary>
        /// Kullanıcının rollerini getirir
        /// </summary>
        [HttpGet("{id}/roles")]
        [ProducesResponseType(typeof(DataResponse<List<string>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<List<string>>>> GetUserRoles(int id)
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync(id);
                if (roles == null)
                    return NotFound(BaseResponse.CreateFailure("Kullanıcı bulunamadı"));

                return Ok(DataResponse<List<string>>.CreateSuccess(roles.ToList(), "Kullanıcı rolleri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} rolleri getirilirken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Roller getirilemedi"));
            }
        }

        /// <summary>
        /// Kullanıcının tüm rollerini ayarlar (mevcut rolleri değiştirir)
        /// </summary>
        [HttpPut("{id}/roles")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<BaseResponse>> SetUserRoles(int id, [FromBody] SetRolesRequestModel model)
        {
            try
            {
                if (model.Roles == null)
                    model.Roles = new List<string>();

                var success = await _userService.SetUserRolesAsync(id, model.Roles);
                if (!success)
                    return BadRequest(BaseResponse.CreateFailure("Kullanıcı rolleri ayarlanamadı"));

                return Ok(BaseResponse.CreateSuccess("Kullanıcı rolleri başarıyla ayarlandı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {UserId} rolleri ayarlanırken hata oluştu", id);
                return StatusCode(500, BaseResponse.CreateFailure("Roller ayarlanamadı"));
            }
        }
    }

    // Request models for role operations
    public class RoleRequestModel
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class SetRolesRequestModel
    {
        public List<string> Roles { get; set; } = new List<string>();
    }
}
