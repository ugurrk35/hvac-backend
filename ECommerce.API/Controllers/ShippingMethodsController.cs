using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/shipping-methods")]
    public class ShippingMethodsController : ControllerBase
    {
        private readonly IShippingMethodRepository _shippingMethods;
        private readonly ApplicationDbContext _db;

        public ShippingMethodsController(IShippingMethodRepository shippingMethods, ApplicationDbContext db)
        {
            _shippingMethods = shippingMethods;
            _db = db;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _db.ShippingCheckoutSettings.AsNoTracking().FirstOrDefaultAsync();
            return Ok(DataResponse<ShippingCheckoutSettingsResponse>.CreateSuccess(
                new(settings?.IsMethodSelectionEnabled ?? false)));
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            var methods = await _shippingMethods.GetActiveAsync();
            var response = methods.Select(method => new ShippingMethodResponse(
                method.Id, method.Name, method.Price, method.FreeShippingThreshold));
            return Ok(DataResponse<IEnumerable<ShippingMethodResponse>>.CreateSuccess(response));
        }
    }

    public record ShippingMethodResponse(int Id, string Name, decimal Price, decimal? FreeShippingThreshold);
    public record ShippingCheckoutSettingsResponse(bool IsMethodSelectionEnabled);
}
