using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/product-campaigns")]
[Authorize(Roles = "Admin")]
public class AdminProductCampaignPackagesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(DataResponse<IEnumerable<ProductCampaignPackageAdminDto>>.CreateSuccess(
        (await Packages().ToListAsync()).Select(ToDto)));


    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);
        var packages = await db.ProductCampaignPackages.AsNoTracking().Where(item => !item.IsDeleted)
            .Select(item => new { item.Id, item.Title }).ToListAsync();
        var eventCounts = await db.ProductCampaignEvents.AsNoTracking().Where(item => !item.IsDeleted && item.CreatedAt >= since && item.ProductCampaignPackageId != null)
            .GroupBy(item => new { PackageId = item.ProductCampaignPackageId!.Value, item.EventType })
            .Select(group => new { group.Key.PackageId, group.Key.EventType, Count = group.Count() }).ToListAsync();
        var conversions = await db.OrderItems.AsNoTracking().Where(item => !item.IsDeleted && item.CreatedAt >= since && item.ProductCampaignPackageId != null)
            .GroupBy(item => item.ProductCampaignPackageId!.Value)
            .Select(group => new { PackageId = group.Key, Orders = group.Select(item => item.OrderId).Distinct().Count(), Revenue = group.Sum(item => item.Price * item.Quantity) }).ToListAsync();
        var result = packages.Select(package => new ProductCampaignAnalyticsDto(
            package.Id, package.Title,
            eventCounts.Where(item => item.PackageId == package.Id).ToDictionary(item => item.EventType, item => item.Count),
            conversions.FirstOrDefault(item => item.PackageId == package.Id)?.Orders ?? 0,
            conversions.FirstOrDefault(item => item.PackageId == package.Id)?.Revenue ?? 0m)).ToList();
        return Ok(DataResponse<IEnumerable<ProductCampaignAnalyticsDto>>.CreateSuccess(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveProductCampaignPackageRequest request)
    {
        try
        {
            var package = await BuildPackage(request);
            db.ProductCampaignPackages.Add(package);
            await db.SaveChangesAsync();
            return Ok(DataResponse<ProductCampaignPackageAdminDto>.CreateSuccess(ToDto(await Packages().FirstAsync(item => item.Id == package.Id))));
        }
        catch (ArgumentException exception) { return BadRequest(BaseResponse.CreateFailure(exception.Message)); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveProductCampaignPackageRequest request)
    {
        try
        {
            var current = await db.ProductCampaignPackages.Include(item => item.LocationRules).Include(item => item.LookupGroups).ThenInclude(item => item.Options).FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
            if (current == null) return NotFound(BaseResponse.CreateFailure("Kampanya paketi bulunamadı."));
            var replacement = await BuildPackage(request);
            current.Title = replacement.Title; current.Description = replacement.Description; current.StartingPrice = replacement.StartingPrice; current.RequiresExistingDevicePhoto = replacement.RequiresExistingDevicePhoto; current.SortOrder = replacement.SortOrder; current.IsActive = replacement.IsActive;
            db.ProductCampaignLocationRules.RemoveRange(current.LocationRules); db.ProductCampaignLookupOptions.RemoveRange(current.LookupGroups.SelectMany(item => item.Options)); db.ProductCampaignLookupGroups.RemoveRange(current.LookupGroups);
            current.LocationRules = replacement.LocationRules; current.LookupGroups = replacement.LookupGroups;
            await db.SaveChangesAsync();
            return Ok(DataResponse<ProductCampaignPackageAdminDto>.CreateSuccess(ToDto(await Packages().FirstAsync(item => item.Id == id))));
        }
        catch (ArgumentException exception) { return BadRequest(BaseResponse.CreateFailure(exception.Message)); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var package = await db.ProductCampaignPackages.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (package == null) return NotFound(BaseResponse.CreateFailure("Kampanya paketi bulunamadı."));
        package.IsDeleted = true; package.IsActive = false; await db.SaveChangesAsync(); return NoContent();
    }

    private IQueryable<ProductCampaignPackage> Packages() => db.ProductCampaignPackages.AsNoTracking().Include(item => item.LocationRules).Include(item => item.LookupGroups).ThenInclude(item => item.Options).Where(item => !item.IsDeleted).OrderBy(item => item.SortOrder).ThenBy(item => item.Title);
    private async Task<ProductCampaignPackage> BuildPackage(SaveProductCampaignPackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.StartingPrice < 0) throw new ArgumentException("Başlık ve geçerli başlangıç fiyatı zorunludur.");
        var codes = request.Groups.Select(item => item.Code?.Trim().ToLowerInvariant()).ToList();
        if (codes.Any(string.IsNullOrWhiteSpace) || codes.Distinct().Count() != codes.Count) throw new ArgumentException("Her lookup grubu için benzersiz bir kod girilmelidir.");
        return new ProductCampaignPackage { Title = request.Title.Trim(), Description = request.Description?.Trim() ?? string.Empty, StartingPrice = request.StartingPrice, RequiresExistingDevicePhoto = request.RequiresExistingDevicePhoto, SortOrder = request.SortOrder, IsActive = request.IsActive,
            LocationRules = request.Locations.Where(item => !string.IsNullOrWhiteSpace(item.City)).Select((item, index) => new ProductCampaignLocationRule { City = item.City.Trim(), District = string.IsNullOrWhiteSpace(item.District) ? null : item.District.Trim(), PriceAdjustment = item.PriceAdjustment, SortOrder = index, IsActive = true }).ToList(),
            LookupGroups = request.Groups.Where(item => !string.IsNullOrWhiteSpace(item.Code) && !string.IsNullOrWhiteSpace(item.Label)).Select((item, index) => new ProductCampaignLookupGroup { Code = item.Code.Trim().ToLowerInvariant(), Label = item.Label.Trim(), IsRequired = item.IsRequired, SortOrder = index, IsActive = true, Options = item.Options.Where(option => !string.IsNullOrWhiteSpace(option.Label)).Select((option, optionIndex) => new ProductCampaignLookupOption { Label = option.Label.Trim(), PriceAdjustment = option.PriceAdjustment, IsDefault = option.IsDefault, SortOrder = optionIndex, IsActive = true }).ToList() }).ToList() };
    }
    private static ProductCampaignPackageAdminDto ToDto(ProductCampaignPackage item) => new(item.Id, item.Title, item.Description, item.StartingPrice, item.RequiresExistingDevicePhoto, item.SortOrder, item.IsActive, item.LocationRules.OrderBy(rule => rule.SortOrder).Select(rule => new ProductCampaignLocationDto(rule.Id, rule.City, rule.District, rule.PriceAdjustment)).ToList(), item.LookupGroups.OrderBy(group => group.SortOrder).Select(group => new ProductCampaignGroupDto(group.Id, group.Code, group.Label, group.IsRequired, group.Options.OrderBy(option => option.SortOrder).Select(option => new ProductCampaignOptionDto(option.Id, option.Label, option.PriceAdjustment, option.IsDefault)).ToList())).ToList());
}

public record SaveProductCampaignPackageRequest(string? Title, string? Description, decimal StartingPrice, bool RequiresExistingDevicePhoto, int SortOrder, bool IsActive, List<SaveProductCampaignLocationRequest> Locations, List<SaveProductCampaignGroupRequest> Groups);
public record SaveProductCampaignLocationRequest(string? City, string? District, decimal PriceAdjustment);
public record SaveProductCampaignGroupRequest(string? Code, string? Label, bool IsRequired, List<SaveProductCampaignOptionRequest> Options);
public record SaveProductCampaignOptionRequest(string? Label, decimal PriceAdjustment, bool IsDefault);
public record ProductCampaignPackageAdminDto(int Id, string Title, string Description, decimal StartingPrice, bool RequiresExistingDevicePhoto, int SortOrder, bool IsActive, List<ProductCampaignLocationDto> Locations, List<ProductCampaignGroupDto> Groups);
public record ProductCampaignLocationDto(int Id, string City, string? District, decimal PriceAdjustment);
public record ProductCampaignGroupDto(int Id, string Code, string Label, bool IsRequired, List<ProductCampaignOptionDto> Options);
public record ProductCampaignOptionDto(int Id, string Label, decimal PriceAdjustment, bool IsDefault);
public record ProductCampaignAnalyticsDto(int PackageId, string Title, Dictionary<string, int> Events, int Orders, decimal Revenue);
