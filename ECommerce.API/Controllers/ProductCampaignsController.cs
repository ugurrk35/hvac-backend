using ECommerce.API.Services;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/product-campaigns")]
public class ProductCampaignsController(ProductCampaignQuoteService campaignQuotes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPackages(CancellationToken cancellationToken)
    {
        var packages = await campaignQuotes.GetActivePackagesAsync(cancellationToken);
        return Ok(DataResponse<object>.CreateSuccess(packages.Select(package => new {
            package.Id, package.Title, package.Description, package.StartingPrice, package.RequiresExistingDevicePhoto,
            locations = package.LocationRules.OrderBy(item => item.SortOrder).ThenBy(item => item.City).Select(item => new { item.Id, item.City, item.District, item.PriceAdjustment }),
            groups = package.LookupGroups.OrderBy(item => item.SortOrder).Select(group => new
            {
                group.Id, group.Code, group.Label, group.IsRequired,
                options = group.Options.OrderBy(item => item.SortOrder).Select(option => new { option.Id, option.Label, option.PriceAdjustment, option.IsDefault })
            })
        })));
    }

    [HttpGet("{packageId:int}/locations")]
    public async Task<IActionResult> GetLocations(int packageId, CancellationToken cancellationToken)
    {
        var package = await campaignQuotes.GetActivePackageByIdAsync(packageId, cancellationToken);
        if (package == null) return NotFound(BaseResponse.CreateFailure("Kampanya paketi bulunamadı."));
        return Ok(DataResponse<object>.CreateSuccess(package.LocationRules.OrderBy(item => item.SortOrder).ThenBy(item => item.City).Select(item => new { item.Id, item.City, item.District, item.PriceAdjustment })));
    }

    [HttpPost("{packageId:int}/quote")]
    public async Task<IActionResult> Quote(int packageId, [FromBody] ProductCampaignQuoteRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(DataResponse<ProductCampaignQuoteResult>.CreateSuccess(await campaignQuotes.QuoteAsync(packageId, request, cancellationToken))); }
        catch (ArgumentException exception) { return BadRequest(BaseResponse.CreateFailure(exception.Message)); }
    }
}
