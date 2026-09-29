using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.UserDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using ECommerce.API.Services;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Kimlik doğrulama ve kullanıcı oturumu işlemlerini yönetir (giriş, kayıt vb.).
    /// JWT tabanlı kimlik doğrulama ile çalışır.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly EmailConfirmationService _emailConfirmationService;
        private readonly IConfiguration _configuration;
        private readonly ITurnstileValidator _turnstileValidator;

        public AuthController(IUserService userService, IEmailService emailService, EmailConfirmationService emailConfirmationService, IConfiguration configuration, ITurnstileValidator turnstileValidator)
        {
            _userService = userService;
            _emailService = emailService;
            _emailConfirmationService = emailConfirmationService;
            _configuration = configuration;
            _turnstileValidator = turnstileValidator;
        }

        public class LoginResponse
        {
            public string Token { get; set; }
            public UserSummaryDto User { get; set; }
        }

        public class UserSummaryDto
        {
            public int Id { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Geçersiz değer." : e.ErrorMessage)
                    .ToList();

                return BadRequest(DataResponse<LoginResponse>.CreateFailure("Geçersiz giriş isteği.", errors));
            }

            var identifier = request.Identifier?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return BadRequest(DataResponse<LoginResponse>.CreateFailure("E-posta veya telefon numarası gereklidir."));
            }

            var loginResult = await _userService.LoginWithUserAsync(identifier, request.Password);
            if (loginResult == null)
            {
                return Unauthorized(DataResponse<LoginResponse>.CreateFailure("E-posta/telefon veya şifre hatalı."));
            }

            var response = new LoginResponse
            {
                Token = loginResult.Value.Token,
                User = new UserSummaryDto
                {
                    Id = loginResult.Value.User.Id,
                    Email = loginResult.Value.User.Email,
                    FirstName = loginResult.Value.User.FirstName,
                    LastName = loginResult.Value.User.LastName
                }
            };

            return Ok(DataResponse<LoginResponse>.CreateSuccess(response, "Giriş başarılı."));
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Geçersiz değer." : e.ErrorMessage)
                    .ToList();

                return BadRequest(DataResponse<UserSummaryDto>.CreateFailure("Geçersiz kayıt isteği.", errors));
            }

            var turnstileResult = await _turnstileValidator.ValidateAsync(
                model.TurnstileToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "register",
                HttpContext.RequestAborted);
            if (!turnstileResult.Success)
            {
                return BadRequest(DataResponse<UserSummaryDto>.CreateFailure("Güvenlik doğrulaması başarısız. Lütfen tekrar deneyin."));
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLowerInvariant(),
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                IsGuest = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userService.CreateUserAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(DataResponse<UserSummaryDto>.CreateFailure("Kullanıcı oluşturulamadı.", errors));
            }

            var token = await _emailConfirmationService.CreateAsync(user.Email, EmailConfirmationPurposes.Account, user.Id, HttpContext.RequestAborted);
            var siteUrl = (_configuration["Email:PublicSiteUrl"] ?? "https://www.kombiklimaburada.com").TrimEnd('/');
            await _emailService.SendEmailVerificationAsync(user, $"{siteUrl}/account/verify-email?token={Uri.EscapeDataString(token)}", HttpContext.RequestAborted);

            var response = new UserSummaryDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return Ok(DataResponse<UserSummaryDto>.CreateSuccess(response, "Hesabınız oluşturuldu. E-posta adresinize gönderilen doğrulama bağlantısını onaylayın."));
        }
    }
}
