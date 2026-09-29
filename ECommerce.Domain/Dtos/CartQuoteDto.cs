namespace ECommerce.Domain.Dtos
{
    /// <summary>Server-authoritative cart price breakdown.</summary>
    public class CartQuoteDto
    {
        public int CartId { get; set; }
        public string Currency { get; set; } = "TRY";
        public decimal MerchandiseSubtotal { get; set; }
        public decimal ProductDiscountTotal { get; set; }
        public decimal CampaignDiscountTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public bool? CouponApplied { get; set; }
        public string? CouponMessage { get; set; }
        public List<string> ProgressMessages { get; set; } = new();
        public List<GiftQuoteItemDto> GiftItems { get; set; } = new();
        public List<AppliedCampaignDto> AppliedCampaigns { get; set; } = new();
        public int? ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public decimal? AmountUntilFreeShipping { get; set; }
        public List<CartQuoteLineDto> Items { get; set; } = new();
    }

    public class AppliedCampaignDto
    {
        public int CampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public bool FreeShipping { get; set; }
        public bool IsCouponCampaign { get; set; }
    }

    public class GiftQuoteItemDto { public int CampaignId { get; set; } public int ProductId { get; set; } public string ProductName { get; set; } = string.Empty; public int Quantity { get; set; } }

    public class CartQuoteLineDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal ListUnitPrice { get; set; }
        public decimal EffectiveUnitPrice { get; set; }
        public decimal LineDiscountTotal { get; set; }
        public decimal LineTotal { get; set; }
    }
}
