using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Service.Concrete
{
    public class PriceQuoteService : IPriceQuoteService
    {
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private readonly IShippingMethodRepository _shippingMethodRepository;
        private readonly ApplicationDbContext _db;

        public PriceQuoteService(IShoppingCartRepository shoppingCartRepository, IShippingMethodRepository shippingMethodRepository, ApplicationDbContext db)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _shippingMethodRepository = shippingMethodRepository;
            _db = db;
        }

        public decimal GetEffectiveUnitPrice(Product product, ProductAttributeCombination? combination = null)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            // Seçili varyantın fiyatı ürün fiyatı ve indiriminden önceliklidir.
            if (combination is { Price: > 0 } && combination.ProductId == product.Id)
                return combination.Price;

            return product.DiscountPrice is > 0 and var discount && discount < product.BasePrice
                ? discount
                : product.BasePrice;
        }

        public async Task<CartQuoteDto> GetCartQuoteAsync(int cartId, int? shippingMethodId = null, string? couponCode = null, string? city = null, string? district = null)
        {
            if (cartId <= 0) throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            var cart = await _shoppingCartRepository.GetCartWithItems(cartId);
            if (cart == null) throw new InvalidOperationException("Sepet bulunamadı.");

            var quote = new CartQuoteDto { CartId = cartId };
            foreach (var item in cart.CartItems ?? Enumerable.Empty<CartItem>())
            {
                if (item.Product == null) throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");

                var hasVariantPrice = item.ProductAttributeCombination is { Price: > 0 } combination &&
                    combination.ProductId == item.ProductId;
                var listUnitPrice = item.UnitPriceSnapshot ?? (hasVariantPrice ? item.ProductAttributeCombination!.Price : item.Product.BasePrice);
                var effectiveUnitPrice = item.UnitPriceSnapshot ?? GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination);
                var listLineTotal = listUnitPrice * item.Quantity;
                var lineTotal = effectiveUnitPrice * item.Quantity;

                quote.Items.Add(new CartQuoteLineDto
                {
                    CartItemId = item.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ListUnitPrice = listUnitPrice,
                    EffectiveUnitPrice = effectiveUnitPrice,
                    LineDiscountTotal = listLineTotal - lineTotal,
                    LineTotal = lineTotal
                });
            }

            quote.MerchandiseSubtotal = quote.Items.Sum(item => item.ListUnitPrice * item.Quantity);
            quote.ProductDiscountTotal = quote.Items.Sum(item => item.LineDiscountTotal);
            var merchandiseTotal = quote.Items.Sum(item => item.LineTotal);
            var now = DateTime.UtcNow;
            var normalizedCoupon = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim().ToUpperInvariant();
            var campaigns = await _db.Campaigns.AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted &&
                    (c.CouponCode == null || (normalizedCoupon != null && c.CouponCode == normalizedCoupon)) &&
                    c.StartsAt <= now && (!c.EndsAt.HasValue || c.EndsAt >= now) && (!c.UsageLimit.HasValue || c.UsageCount < c.UsageLimit))
                .OrderByDescending(c => c.Priority)
                .ToListAsync();
            var customerCampaignUsage = new Dictionary<int, int>();
            var hasPaidOrder = false;
            if (cart.UserId.HasValue || cart.GuestIdentifier.HasValue)
            {
                var paidOrders = await _db.Orders.AsNoTracking().Where(order => order.PaymentStatusId == (int)PaymentStatus.Paid && order.AppliedCampaignIds != null && (cart.UserId.HasValue ? order.UserId == cart.UserId : order.GuestIdentifier == cart.GuestIdentifier)).Select(order => order.AppliedCampaignIds!).ToListAsync();
                hasPaidOrder = await _db.Orders.AsNoTracking().AnyAsync(order => order.PaymentStatusId == (int)PaymentStatus.Paid && (cart.UserId.HasValue ? order.UserId == cart.UserId : order.GuestIdentifier == cart.GuestIdentifier));
                foreach (var id in paidOrders.SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)).Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0)) customerCampaignUsage[id] = customerCampaignUsage.GetValueOrDefault(id) + 1;
            }
            var nonStackableApplied = false;
            var freeShippingCampaign = false;
            foreach (var campaign in campaigns)
            {
                if (nonStackableApplied) break;
                if (campaign.UsageLimitPerCustomer.HasValue && customerCampaignUsage.GetValueOrDefault(campaign.Id) >= campaign.UsageLimitPerCustomer.Value) continue;
                if (campaign.FirstOrderOnly && hasPaidOrder) continue;
                if (campaign.MinimumCartAmount.HasValue && merchandiseTotal < campaign.MinimumCartAmount.Value) continue;
                var matchingItems = cart.CartItems.Where(item =>
                    (!campaign.TargetProductId.HasValue || item.ProductId == campaign.TargetProductId.Value) &&
                    (!campaign.TargetCategoryId.HasValue || item.Product.CategoryId == campaign.TargetCategoryId.Value) &&
                    (!campaign.TargetProductTagId.HasValue || item.Product.ProductProductTags.Any(tag => tag.ProductTagId == campaign.TargetProductTagId.Value))).ToList();
                if ((campaign.TargetProductId.HasValue || campaign.TargetCategoryId.HasValue || campaign.TargetProductTagId.HasValue) && matchingItems.Count == 0) continue;
                if (campaign.MinimumQuantity.HasValue && matchingItems.Sum(item => item.Quantity) < campaign.MinimumQuantity.Value) continue;

                var discount = campaign.RewardType switch
                {
                    CampaignRewardType.PercentageDiscount => Math.Min(merchandiseTotal, merchandiseTotal * campaign.RewardValue / 100m),
                    CampaignRewardType.FixedDiscount => Math.Min(merchandiseTotal, campaign.RewardValue),
                    CampaignRewardType.BuyXPayY => CalculateBuyXPayYDiscount(matchingItems, campaign.MinimumQuantity, campaign.PayQuantity),
                    CampaignRewardType.Bundle => CalculateBundleDiscount(cart.CartItems, campaign.BundleProductIds, campaign.BundlePrice),
                    _ => 0m
                };
                if (campaign.RewardType == CampaignRewardType.GiftProduct)
                {
                    if (!campaign.RewardProductId.HasValue) continue;
                    var gift = await _db.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == campaign.RewardProductId.Value && product.IsActive && product.IsPublished && !product.IsDeleted && product.Quantity > 0);
                    if (gift == null) continue;
                    quote.GiftItems.Add(new GiftQuoteItemDto { CampaignId = campaign.Id, ProductId = gift.Id, ProductName = gift.Name, Quantity = Math.Max(1, (int)campaign.RewardValue) });
                    quote.AppliedCampaigns.Add(new AppliedCampaignDto { CampaignId = campaign.Id, Name = campaign.Name, IsCouponCampaign = campaign.CouponCode != null });
                    nonStackableApplied |= !campaign.IsStackable;
                    continue;
                }
                var isFreeShippingCampaign = campaign.RewardType == CampaignRewardType.FreeShipping;
                if (discount <= 0m && !isFreeShippingCampaign) continue;
                freeShippingCampaign |= isFreeShippingCampaign;
                quote.CampaignDiscountTotal += discount;
                merchandiseTotal -= discount;
                quote.AppliedCampaigns.Add(new AppliedCampaignDto { CampaignId = campaign.Id, Name = campaign.Name, DiscountAmount = discount, FreeShipping = campaign.RewardType == CampaignRewardType.FreeShipping, IsCouponCampaign = campaign.CouponCode != null });
                nonStackableApplied |= !campaign.IsStackable;
            }

            if (normalizedCoupon != null)
            {
                quote.CouponApplied = quote.AppliedCampaigns.Any(campaign => campaign.IsCouponCampaign);
                quote.CouponMessage = quote.CouponApplied.Value
                    ? "Kupon uygulandı."
                    : campaigns.Any(campaign => campaign.CouponCode == normalizedCoupon)
                        ? "Kuponun sepet koşulları henüz sağlanmıyor."
                        : "Kupon geçersiz, süresi dolmuş veya kullanım limiti dolmuş.";
            }

            quote.ProgressMessages = campaigns
                .Where(campaign => campaign.CouponCode == null && campaign.MinimumCartAmount.HasValue && campaign.MinimumCartAmount.Value > merchandiseTotal)
                .OrderBy(campaign => campaign.MinimumCartAmount)
                .Take(2)
                .Select(campaign => $"{campaign.Name} için sepetinize {(campaign.MinimumCartAmount!.Value - merchandiseTotal):0.##} ₺ daha ekleyin.")
                .ToList();

            if (shippingMethodId.HasValue)
            {
                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(shippingMethodId.Value);
                if (shippingMethod == null || !shippingMethod.IsActive || shippingMethod.IsDeleted)
                    throw new InvalidOperationException("Geçerli bir kargo yöntemi seçilmelidir.");

                var normalizedCity = city?.Trim();
                var normalizedDistrict = district?.Trim();
                var rateOverride = string.IsNullOrWhiteSpace(normalizedCity) ? null : await _db.ShippingMethodRateOverrides.AsNoTracking()
                    .Where(rate => rate.ShippingMethodId == shippingMethod.Id && rate.IsActive && !rate.IsDeleted && rate.City == normalizedCity &&
                        (rate.District == null || rate.District == normalizedDistrict))
                    .OrderByDescending(rate => rate.District != null)
                    .FirstOrDefaultAsync();
                var shippingPrice = rateOverride?.Price ?? shippingMethod.Price;
                var freeShippingThreshold = rateOverride?.FreeShippingThreshold ?? shippingMethod.FreeShippingThreshold;

                quote.ShippingMethodId = shippingMethod.Id;
                quote.ShippingMethodName = shippingMethod.Name;
                quote.FreeShippingThreshold = freeShippingThreshold;
                quote.AmountUntilFreeShipping = freeShippingThreshold.HasValue
                    ? Math.Max(0m, freeShippingThreshold.Value - merchandiseTotal)
                    : null;
            var qualifiesForFreeShipping = freeShippingCampaign ||
                (freeShippingThreshold.HasValue &&
                 merchandiseTotal >= freeShippingThreshold.Value);
            quote.ShippingTotal = qualifiesForFreeShipping ? 0m : shippingPrice;
            }

            quote.GrandTotal = merchandiseTotal + quote.ShippingTotal;

            return quote;
        }

        private decimal CalculateBuyXPayYDiscount(List<CartItem> items, int? buyQuantity, int? payQuantity)
        {
            if (!buyQuantity.HasValue || !payQuantity.HasValue || buyQuantity.Value <= payQuantity.Value || payQuantity.Value < 0) return 0m;
            var totalQuantity = items.Sum(item => item.Quantity); var freeItemCount = totalQuantity / buyQuantity.Value * (buyQuantity.Value - payQuantity.Value);
            return items.OrderBy(item => item.UnitPriceSnapshot ?? GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination)).SelectMany(item => Enumerable.Repeat(item.UnitPriceSnapshot ?? GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination), item.Quantity)).Take(freeItemCount).Sum();
        }

        private decimal CalculateBundleDiscount(ICollection<CartItem> cartItems, string? bundleProductIds, decimal? bundlePrice)
        {
            if (string.IsNullOrWhiteSpace(bundleProductIds) || !bundlePrice.HasValue || bundlePrice < 0) return 0m;
            var productIds = bundleProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => int.TryParse(value.Trim(), out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();
            if (productIds.Count < 2) return 0m;
            var bundleItems = cartItems.Where(item => productIds.Contains(item.ProductId)).GroupBy(item => item.ProductId).Select(group => new { Quantity = group.Sum(item => item.Quantity), UnitPrice = group.First().UnitPriceSnapshot ?? GetEffectiveUnitPrice(group.First().Product, group.First().ProductAttributeCombination) }).ToList();
            if (bundleItems.Count != productIds.Count) return 0m;
            var bundleCount = bundleItems.Min(item => item.Quantity);
            var regularBundlePrice = bundleItems.Sum(item => item.UnitPrice);
            return Math.Max(0m, (regularBundlePrice - bundlePrice.Value) * bundleCount);
        }
    }
}
