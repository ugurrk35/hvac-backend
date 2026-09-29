using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/shipping-methods")]
    [Authorize(Roles = "Admin")]
    public class AdminShippingMethodsController : ControllerBase
    {
        private readonly IShippingMethodRepository _shippingMethods;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;

        public AdminShippingMethodsController(IShippingMethodRepository shippingMethods, IUnitOfWork unitOfWork, ApplicationDbContext db)
        {
            _shippingMethods = shippingMethods;
            _unitOfWork = unitOfWork;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var methods = (await _shippingMethods.GetAllAsync())
                .Where(method => !method.IsDeleted)
                .OrderBy(method => method.Price)
                .Select(Map);
            return Ok(DataResponse<IEnumerable<AdminShippingMethodResponse>>.CreateSuccess(methods));
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _db.ShippingCheckoutSettings.AsNoTracking().FirstOrDefaultAsync();
            return Ok(DataResponse<ShippingCheckoutSettingsResponse>.CreateSuccess(
                new(settings?.IsMethodSelectionEnabled ?? false)));
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateShippingCheckoutSettingsRequest request)
        {
            var settings = await _db.ShippingCheckoutSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new ShippingCheckoutSettings();
                _db.ShippingCheckoutSettings.Add(settings);
            }

            settings.IsMethodSelectionEnabled = request.IsMethodSelectionEnabled;
            await _db.SaveChangesAsync();
            return Ok(DataResponse<ShippingCheckoutSettingsResponse>.CreateSuccess(
                new(settings.IsMethodSelectionEnabled)));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertShippingMethodRequest request)
        {
            var error = Validate(request);
            if (error != null) return BadRequest(BaseResponse.CreateFailure(error));

            var method = new ShippingMethod
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                FreeShippingThreshold = request.FreeShippingThreshold,
                TrackingUrl = request.TrackingUrl?.Trim() ?? string.Empty,
                IsActive = request.IsActive
            };
            await _shippingMethods.AddAsync(method);
            await _unitOfWork.CompleteAsync();
            return CreatedAtAction(nameof(GetAll), DataResponse<AdminShippingMethodResponse>.CreateSuccess(Map(method)));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertShippingMethodRequest request)
        {
            var error = Validate(request);
            if (error != null) return BadRequest(BaseResponse.CreateFailure(error));

            var method = await _shippingMethods.GetByIdAsync(id);
            if (method == null || method.IsDeleted) return NotFound(BaseResponse.CreateFailure("Kargo yöntemi bulunamadı."));

            method.Name = request.Name.Trim();
            method.Price = request.Price;
            method.FreeShippingThreshold = request.FreeShippingThreshold;
            method.TrackingUrl = request.TrackingUrl?.Trim() ?? string.Empty;
            method.IsActive = request.IsActive;
            await _shippingMethods.UpdateAsync(method);
            await _unitOfWork.CompleteAsync();
            return Ok(DataResponse<AdminShippingMethodResponse>.CreateSuccess(Map(method)));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var method = await _shippingMethods.GetByIdAsync(id);
            if (method == null || method.IsDeleted) return NotFound(BaseResponse.CreateFailure("Kargo yöntemi bulunamadı."));

            method.IsActive = false;
            method.IsDeleted = true;
            await _shippingMethods.UpdateAsync(method);
            await _unitOfWork.CompleteAsync();
            return Ok(BaseResponse.CreateSuccess("Kargo yöntemi silindi."));
        }

        [HttpGet("{shippingMethodId:int}/rate-overrides")]
        public async Task<IActionResult> GetRateOverrides(int shippingMethodId) => Ok(DataResponse<IEnumerable<ShippingRateOverrideResponse>>.CreateSuccess(
            await _db.ShippingMethodRateOverrides.AsNoTracking().Where(rate => rate.ShippingMethodId == shippingMethodId && !rate.IsDeleted)
                .OrderBy(rate => rate.City).ThenBy(rate => rate.District).Select(rate => new ShippingRateOverrideResponse(rate.Id, rate.City, rate.District, rate.Price, rate.FreeShippingThreshold, rate.IsActive)).ToListAsync()));

        [HttpPost("{shippingMethodId:int}/rate-overrides")]
        public async Task<IActionResult> CreateRateOverride(int shippingMethodId, [FromBody] UpsertShippingRateOverrideRequest request)
        {
            if (!await _db.ShippingMethods.AnyAsync(method => method.Id == shippingMethodId && !method.IsDeleted)) return NotFound(BaseResponse.CreateFailure("Kargo yöntemi bulunamadı."));
            var error = ValidateRateOverride(request); if (error != null) return BadRequest(BaseResponse.CreateFailure(error));
            var rate = new ShippingMethodRateOverride { ShippingMethodId = shippingMethodId, City = request.City.Trim(), District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(), Price = request.Price, FreeShippingThreshold = request.FreeShippingThreshold, IsActive = request.IsActive };
            _db.ShippingMethodRateOverrides.Add(rate); await _db.SaveChangesAsync();
            return Ok(DataResponse<ShippingRateOverrideResponse>.CreateSuccess(new(rate.Id, rate.City, rate.District, rate.Price, rate.FreeShippingThreshold, rate.IsActive)));
        }

        [HttpPut("rate-overrides/{id:int}")]
        public async Task<IActionResult> UpdateRateOverride(int id, [FromBody] UpsertShippingRateOverrideRequest request)
        {
            var rate = await _db.ShippingMethodRateOverrides.FindAsync(id); if (rate == null || rate.IsDeleted) return NotFound(BaseResponse.CreateFailure("Bölgesel kargo kuralı bulunamadı."));
            var error = ValidateRateOverride(request); if (error != null) return BadRequest(BaseResponse.CreateFailure(error));
            rate.City = request.City.Trim(); rate.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(); rate.Price = request.Price; rate.FreeShippingThreshold = request.FreeShippingThreshold; rate.IsActive = request.IsActive; await _db.SaveChangesAsync();
            return Ok(DataResponse<ShippingRateOverrideResponse>.CreateSuccess(new(rate.Id, rate.City, rate.District, rate.Price, rate.FreeShippingThreshold, rate.IsActive)));
        }

        [HttpDelete("rate-overrides/{id:int}")]
        public async Task<IActionResult> DeleteRateOverride(int id)
        {
            var rate = await _db.ShippingMethodRateOverrides.FindAsync(id); if (rate == null || rate.IsDeleted) return NotFound(BaseResponse.CreateFailure("Bölgesel kargo kuralı bulunamadı."));
            rate.IsActive = false; rate.IsDeleted = true; await _db.SaveChangesAsync(); return Ok(BaseResponse.CreateSuccess("Bölgesel kargo kuralı silindi."));
        }

        private static string? Validate(UpsertShippingMethodRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return "Kargo yöntemi adı zorunludur.";
            if (request.Price < 0) return "Kargo ücreti negatif olamaz.";
            if (request.FreeShippingThreshold < 0) return "Ücretsiz kargo eşiği negatif olamaz.";
            return null;
        }

        private static string? ValidateRateOverride(UpsertShippingRateOverrideRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.City)) return "İl zorunludur.";
            if (request.Price < 0 || request.FreeShippingThreshold < 0) return "Kargo ücreti ve eşik negatif olamaz.";
            if (!request.Price.HasValue && !request.FreeShippingThreshold.HasValue) return "Ücret veya ücretsiz kargo eşiği girilmelidir.";
            return null;
        }

        private static AdminShippingMethodResponse Map(ShippingMethod method) => new(
            method.Id, method.Name, method.Price, method.FreeShippingThreshold, method.TrackingUrl, method.IsActive);
    }

    public record UpsertShippingMethodRequest(string Name, decimal Price, decimal? FreeShippingThreshold, string? TrackingUrl, bool IsActive);
    public record AdminShippingMethodResponse(int Id, string Name, decimal Price, decimal? FreeShippingThreshold, string TrackingUrl, bool IsActive);
    public record UpsertShippingRateOverrideRequest(string City, string? District, decimal? Price, decimal? FreeShippingThreshold, bool IsActive);
    public record ShippingRateOverrideResponse(int Id, string City, string? District, decimal? Price, decimal? FreeShippingThreshold, bool IsActive);
    public record UpdateShippingCheckoutSettingsRequest(bool IsMethodSelectionEnabled);
    public record ShippingCheckoutSettingsResponse(bool IsMethodSelectionEnabled);
}
