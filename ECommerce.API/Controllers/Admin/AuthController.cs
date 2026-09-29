using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Concrete;
using ECommerce.Service.Dtos.UserDtos;
using ECommerce.Service.Mapping.Manual;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuthController : ControllerBase
    {
        //private readonly UserService _userService;
        private readonly IUserService _userService;
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        // Kullanıcı kayıt (Register)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Username ?? model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName


                // Gerekirse diğer alanlar buraya eklenebilir
            };

            var createdUser = await _userService.CreateUserAsync(user, model.Password);
            if (createdUser == null)
                return BadRequest("Kullanıcı oluşturulamadı.");

            return Ok("Kullanıcı başarıyla oluşturuldu.");
        }

        // Kullanıcı giriş (Login) - JWT token döner
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var token = await _userService.LoginAsync(model.Username ?? model.Identifier ?? string.Empty, model.Password);
            if (token == null)
                return Unauthorized("Kullanıcı adı veya şifre yanlış.");

            return Ok(new { Token = token });
        }

        // Tüm kullanıcıları listele (Admin veya yetkili)
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users.Select(user => user.ToDto()).ToList());
        }

        // ID'ye göre kullanıcı getir
        [Authorize(Roles = "Admin")]
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user.ToDto());
        }

        //// Kullanıcı güncelle
        //[Authorize]
        //[HttpPut("users/{id}")]
        //public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequestModel model)
        //{
        //    var user = await _userService.GetByIdAsync(id);
        //    if (user == null)
        //        return NotFound();

        //    // Güncellenecek alanlar
        //    user.Email = model.Email ?? user.Email;
        //    user.UserName = model.Username ?? user.UserName;
        //    // Diğer alanlar eklenebilir

        //    await _userService.UpdateUserAsync(user);
        //    return Ok("Kullanıcı güncellendi.");
        //}

        // Kullanıcı sil
        [Authorize(Roles = "Admin")]
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok("Kullanıcı silindi.");
        }

        // Rol oluştur
        [Authorize(Roles = "Admin")]
        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] RoleRequestModel model)
        {
            var success = await _userService.AddRoleAsync(model.RoleName);
            if (!success)
                return BadRequest("Rol oluşturulamadı veya zaten mevcut.");

            return Ok("Rol başarıyla oluşturuldu.");
        }

        // Kullanıcıya rol ata
        [Authorize(Roles = "Admin")]
        [HttpPost("users/{id}/roles")]
        public async Task<IActionResult> AddUserToRole(int id, [FromBody] RoleRequestModel model)
        {
            var success = await _userService.AddUserToRoleAsync(id, model.RoleName);
            if (!success)
                return BadRequest("Kullanıcıya rol atama başarısız.");

            return Ok("Rol başarıyla atandı.");
        }

        // Kullanıcının rollerini getir
        [Authorize(Roles = "Admin")]
        [HttpGet("users/{id}/roles")]
        public async Task<IActionResult> GetUserRoles(int id)
        {
            var roles = await _userService.GetUserRolesAsync(id);
            if (roles == null)
                return NotFound("Kullanıcı bulunamadı.");

            return Ok(roles);
        }
    }
}
