using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed class ProductCampaignQuoteService(ApplicationDbContext db)
{
    public async Task<ProductCampaignPackage?> GetActivePackageByIdAsync(int packageId, CancellationToken cancellationToken = default) =>
        await db.ProductCampaignPackages
            .Include(item => item.LocationRules.Where(rule => rule.IsActive && !rule.IsDeleted))
            .Include(item => item.LookupGroups.Where(group => group.IsActive && !group.IsDeleted))
                .ThenInclude(group => group.Options.Where(option => option.IsActive && !option.IsDeleted))
            .FirstOrDefaultAsync(item => item.Id == packageId && item.IsActive && !item.IsDeleted, cancellationToken);

    public async Task<ProductCampaignPackage?> GetActivePackageAsync(int productId, CancellationToken cancellationToken = default) =>
        await db.ProductCampaignPackages
            .Include(item => item.LocationRules.Where(rule => rule.IsActive && !rule.IsDeleted))
            .Include(item => item.LookupGroups.Where(group => group.IsActive && !group.IsDeleted))
                .ThenInclude(group => group.Options.Where(option => option.IsActive && !option.IsDeleted))
            .Where(item => item.ProductId == productId && item.IsActive && !item.IsDeleted)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductCampaignQuoteResult> QuoteAsync(int packageId, ProductCampaignQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var package = await db.ProductCampaignPackages
            .Include(item => item.Product)
            .Include(item => item.LocationRules.Where(rule => rule.IsActive && !rule.IsDeleted))
            .Include(item => item.LookupGroups.Where(group => group.IsActive && !group.IsDeleted))
                .ThenInclude(group => group.Options.Where(option => option.IsActive && !option.IsDeleted))
            .FirstOrDefaultAsync(item => item.Id == packageId && item.IsActive && !item.IsDeleted, cancellationToken)
            ?? throw new ArgumentException("Kampanya paketi bulunamadı.");

        if (!package.Product.IsActive || !package.Product.IsPublished || package.Product.IsDeleted)
            throw new ArgumentException("Bu ürün şu anda kampanyalı satın almaya uygun değil.");

        var city = request.City?.Trim();
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("Kampanyalı satın alma için şehir seçmelisiniz.");
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        var location = package.LocationRules
            .Where(rule => string.Equals(rule.City, city, StringComparison.OrdinalIgnoreCase) &&
                (rule.District == null || string.Equals(rule.District, district, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(rule => rule.District != null)
            .ThenBy(rule => rule.SortOrder)
            .FirstOrDefault();
        if (location == null) throw new ArgumentException("Seçtiğiniz lokasyon bu kampanya için uygun değil.");

        var optionIds = (request.OptionIds ?? []).Where(id => id > 0).Distinct().ToHashSet();
        var allOptions = package.LookupGroups.SelectMany(group => group.Options).ToList();
        if (optionIds.Any(id => allOptions.All(option => option.Id != id)))
            throw new ArgumentException("Geçersiz kampanya seçeneği gönderildi.");
        var selections = new List<ProductCampaignQuoteSelection>();
        foreach (var group in package.LookupGroups.OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
        {
            var selectedOptions = group.Options.Where(option => optionIds.Contains(option.Id)).ToList();
            if (selectedOptions.Count > 1) throw new ArgumentException($"{group.Label} için yalnızca bir seçim yapabilirsiniz.");
            var selected = selectedOptions.SingleOrDefault();
            if (selected == null) selected = group.Options.OrderByDescending(option => option.IsDefault).ThenBy(option => option.SortOrder).FirstOrDefault();
            if (group.IsRequired && selected == null) throw new ArgumentException($"{group.Label} seçimi zorunludur.");
            if (selected != null) selections.Add(new ProductCampaignQuoteSelection(group.Id, group.Code, group.Label, selected.Id, selected.Label, selected.PriceAdjustment));
        }

        var total = package.StartingPrice + location.PriceAdjustment + selections.Sum(item => item.PriceAdjustment);
        if (total < 0) throw new ArgumentException("Kampanya toplamı geçersiz.");
        return new ProductCampaignQuoteResult(package.Id, package.ProductId, package.Title, package.StartingPrice, city, district, location.PriceAdjustment, selections, total);
    }
}

public sealed record ProductCampaignQuoteRequest(string? City, string? District, IReadOnlyList<int>? OptionIds);
public sealed record ProductCampaignQuoteSelection(int GroupId, string Code, string GroupLabel, int OptionId, string OptionLabel, decimal PriceAdjustment);
public sealed record ProductCampaignQuoteResult(int PackageId, int ProductId, string Title, decimal StartingPrice, string City, string? District, decimal LocationAdjustment, IReadOnlyList<ProductCampaignQuoteSelection> Selections, decimal Total);
