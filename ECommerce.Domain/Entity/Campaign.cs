namespace ECommerce.Domain.Entity
{
    public enum CampaignRewardType { PercentageDiscount = 0, FixedDiscount = 1, FreeShipping = 2, GiftProduct = 3, BuyXPayY = 4, Bundle = 5 }

    /// <summary>Admin-managed campaign definition. Rules are evaluated by the quote service.</summary>
    public class Campaign : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CouponCode { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        public decimal? MinimumCartAmount { get; set; }
        public int? MinimumQuantity { get; set; }
        public int? PayQuantity { get; set; }
        public string? BundleProductIds { get; set; }
        public decimal? BundlePrice { get; set; }
        public int? TargetProductId { get; set; }
        public int? TargetCategoryId { get; set; }
        public int? TargetProductTagId { get; set; }
        public bool FirstOrderOnly { get; set; }
        public CampaignRewardType RewardType { get; set; }
        public decimal RewardValue { get; set; }
        public int? RewardProductId { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageLimitPerCustomer { get; set; }
        public int? ParentCampaignId { get; set; }
        public int UsageCount { get; set; }
    }
}
