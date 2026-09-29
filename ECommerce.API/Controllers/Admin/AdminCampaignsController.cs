using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/campaigns")]
    [Authorize(Roles = "Admin")]
    public class AdminCampaignsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AdminCampaignsController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(DataResponse<IEnumerable<Campaign>>.CreateSuccess(
            await _db.Campaigns.Where(x => !x.IsDeleted).OrderByDescending(x => x.Priority).ThenByDescending(x => x.StartsAt).ToListAsync()));

        [HttpGet("report")]
        public async Task<IActionResult> GetReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var campaigns = await _db.Campaigns.AsNoTracking().Where(campaign => !campaign.IsDeleted)
                .Select(campaign => new { campaign.Id, campaign.Name, campaign.UsageCount }).ToListAsync();
            var paidOrders = await _db.Orders.AsNoTracking()
                .Where(order => !order.IsDeleted && order.PaymentStatusId == (int)PaymentStatus.Paid && order.AppliedCampaignIds != null && (!from.HasValue || order.CreatedAt >= from.Value) && (!to.HasValue || order.CreatedAt < to.Value.AddDays(1)))
                .Select(order => new { order.AppliedCampaignIds, order.CampaignDiscountTotal, order.TotalAmount }).ToListAsync();

            var report = campaigns.Select(campaign =>
            {
                var matchingOrders = paidOrders.Where(order => (order.AppliedCampaignIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(campaign.Id.ToString())).ToList();
                return new CampaignReportDto
                {
                    CampaignId = campaign.Id,
                    CampaignName = campaign.Name,
                    SuccessfulOrderCount = matchingOrders.Count,
                    UsageCount = campaign.UsageCount,
                    AttributedDiscountTotal = matchingOrders.Sum(order => order.CampaignDiscountTotal / Math.Max(1, (order.AppliedCampaignIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Length)),
                    AttributedRevenue = matchingOrders.Sum(order => order.TotalAmount / Math.Max(1, (order.AppliedCampaignIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Length))
                };
            });
            return Ok(DataResponse<IEnumerable<CampaignReportDto>>.CreateSuccess(report));
        }

        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups([FromQuery] string? q = null)
        {
            var term = (q ?? string.Empty).Trim().ToLower();
            var products = await _db.Products.AsNoTracking().Where(x => !x.IsDeleted && (term == string.Empty || x.Name.ToLower().Contains(term))).OrderBy(x => x.Name).Take(50).Select(x => new { x.Id, x.Name }).ToListAsync();
            var categories = await _db.Categories.AsNoTracking().Where(x => !x.IsDeleted && (term == string.Empty || x.Name.ToLower().Contains(term))).OrderBy(x => x.Name).Take(50).Select(x => new { x.Id, x.Name }).ToListAsync();
            var tags = await _db.ProductTags.AsNoTracking().Where(x => !x.IsDeleted && (term == string.Empty || x.Name.ToLower().Contains(term))).OrderBy(x => x.Name).Take(50).Select(x => new { x.Id, x.Name }).ToListAsync();
            return Ok(new { products, categories, tags });
        }

        [HttpPost("{id:int}/coupon-codes")]
        public async Task<IActionResult> GenerateCouponCodes(int id, [FromBody] GenerateCouponCodesRequest request)
        {
            if (request.Count is < 1 or > 1000 || string.IsNullOrWhiteSpace(request.Prefix)) return BadRequest(BaseResponse.CreateFailure("1-1000 arası adet ve kupon öneki zorunludur."));
            var template = await _db.Campaigns.FindAsync(id); if (template == null || template.IsDeleted) return NotFound(BaseResponse.CreateFailure("Kampanya bulunamadı."));
            var prefix = new string(request.Prefix.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).Take(20).ToArray()); if (string.IsNullOrWhiteSpace(prefix)) return BadRequest(BaseResponse.CreateFailure("Kupon öneki geçersiz."));
            var coupons = Enumerable.Range(0, request.Count).Select(_ => new Campaign { Name = template.Name, Description = template.Description, CouponCode = $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 50)], ParentCampaignId = template.Id, StartsAt = template.StartsAt, EndsAt = template.EndsAt, Priority = template.Priority, IsStackable = template.IsStackable, MinimumCartAmount = template.MinimumCartAmount, MinimumQuantity = template.MinimumQuantity, PayQuantity = template.PayQuantity, TargetProductId = template.TargetProductId, TargetCategoryId = template.TargetCategoryId, RewardType = template.RewardType, RewardValue = template.RewardValue, RewardProductId = template.RewardProductId, BundleProductIds = template.BundleProductIds, BundlePrice = template.BundlePrice, UsageLimit = 1, UsageLimitPerCustomer = 1, IsActive = template.IsActive }).ToList();
            _db.Campaigns.AddRange(coupons); await _db.SaveChangesAsync();
            return Ok(DataResponse<IEnumerable<string>>.CreateSuccess(coupons.Select(coupon => coupon.CouponCode!)));
        }

        [HttpGet("{id:int}/coupon-codes.csv")]
        public async Task<IActionResult> ExportCouponCodes(int id)
        {
            var codes = await _db.Campaigns.AsNoTracking().Where(campaign => campaign.ParentCampaignId == id && !campaign.IsDeleted).OrderBy(campaign => campaign.CreatedAt).Select(campaign => campaign.CouponCode).ToListAsync();
            var csv = "Kupon Kodu\n" + string.Join("\n", codes.Select(code => $"\"{code}\""));
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv", $"kampanya-{id}-kuponlari.csv");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Campaign campaign)
        {
            if (string.IsNullOrWhiteSpace(campaign.Name) || campaign.RewardValue < 0) return BadRequest(BaseResponse.CreateFailure("Kampanya adı ve ödül değeri geçerlidir."));
            if (campaign.RewardType == CampaignRewardType.GiftProduct && !campaign.RewardProductId.HasValue) return BadRequest(BaseResponse.CreateFailure("Hediye ürün kampanyası için hediye ürün seçilmelidir."));
            if (campaign.RewardType == CampaignRewardType.BuyXPayY && (!campaign.MinimumQuantity.HasValue || !campaign.PayQuantity.HasValue || campaign.MinimumQuantity <= campaign.PayQuantity)) return BadRequest(BaseResponse.CreateFailure("X al Y öde için X, Y'den büyük olmalıdır."));
            if (campaign.RewardType == CampaignRewardType.Bundle && (string.IsNullOrWhiteSpace(campaign.BundleProductIds) || !campaign.BundlePrice.HasValue || campaign.BundlePrice < 0)) return BadRequest(BaseResponse.CreateFailure("Set için ürün ID listesi ve set fiyatı zorunludur."));
            campaign.CouponCode = string.IsNullOrWhiteSpace(campaign.CouponCode) ? null : campaign.CouponCode.Trim().ToUpperInvariant();
            campaign.Id = 0; campaign.IsActive = true; campaign.IsDeleted = false; campaign.UsageCount = 0;
            await _db.Campaigns.AddAsync(campaign); await _db.SaveChangesAsync();
            return Ok(DataResponse<Campaign>.CreateSuccess(campaign));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Campaign input)
        {
            var campaign = await _db.Campaigns.FindAsync(id);
            if (campaign == null || campaign.IsDeleted) return NotFound(BaseResponse.CreateFailure("Kampanya bulunamadı."));
            if (string.IsNullOrWhiteSpace(input.Name) || input.RewardValue < 0) return BadRequest(BaseResponse.CreateFailure("Kampanya adı ve ödül değeri geçerlidir."));
            if (input.RewardType == CampaignRewardType.GiftProduct && !input.RewardProductId.HasValue) return BadRequest(BaseResponse.CreateFailure("Hediye ürün kampanyası için hediye ürün seçilmelidir."));
            if (input.RewardType == CampaignRewardType.BuyXPayY && (!input.MinimumQuantity.HasValue || !input.PayQuantity.HasValue || input.MinimumQuantity <= input.PayQuantity)) return BadRequest(BaseResponse.CreateFailure("X al Y öde için X, Y'den büyük olmalıdır."));
            if (input.RewardType == CampaignRewardType.Bundle && (string.IsNullOrWhiteSpace(input.BundleProductIds) || !input.BundlePrice.HasValue || input.BundlePrice < 0)) return BadRequest(BaseResponse.CreateFailure("Set için ürün ID listesi ve set fiyatı zorunludur."));
            input.CouponCode = string.IsNullOrWhiteSpace(input.CouponCode) ? null : input.CouponCode.Trim().ToUpperInvariant();
            input.Id = id; input.CreatedAt = campaign.CreatedAt; input.CreatedBy = campaign.CreatedBy; input.UsageCount = campaign.UsageCount; input.IsDeleted = false;
            _db.Entry(campaign).CurrentValues.SetValues(input); await _db.SaveChangesAsync();
            return Ok(DataResponse<Campaign>.CreateSuccess(campaign));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _db.Campaigns.FindAsync(id);
            if (campaign == null) return NotFound(BaseResponse.CreateFailure("Kampanya bulunamadı."));
            campaign.IsActive = false; campaign.IsDeleted = true; await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Kampanya silindi."));
        }
    }

    public class CampaignReportDto
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int SuccessfulOrderCount { get; set; }
        public decimal AttributedDiscountTotal { get; set; }
        public decimal AttributedRevenue { get; set; }
    }
    public record GenerateCouponCodesRequest(string Prefix, int Count);
}
