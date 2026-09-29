namespace ECommerce.API.Models;

public class ProductDetailConfigDto
{
    public string BackgroundColor { get; set; } = "#FAF6F0";
    public string AccentColor { get; set; } = "#A9764F";
    public string CtaColor { get; set; } = "#4F6350";
    public string BlushColor { get; set; } = "#F3DDD5";
    public string PersonalizationBadgeText { get; set; } = "İsimle Kişiselleştirilebilir";
    public string NewBadgeText { get; set; } = "Yeni";
    public bool ShowNewBadge { get; set; } = true;
    public string EmptyReviewText { get; set; } = "Henüz değerlendirilmedi";
    public string ReviewInviteText { get; set; } = "İlk yorumu siz yazın";
    public List<ProductDetailInfoItemDto> TrustItems { get; set; } = Defaults.TrustItems();
    public List<ProductDetailInfoItemDto> FeatureItems { get; set; } = Defaults.FeatureItems();

    public static ProductDetailConfigDto CreateDefault() => new();

    private static class Defaults
    {
        public static List<ProductDetailInfoItemDto> TrustItems() =>
        [
            new() { Icon = "truck", Title = "Hızlı kargo" },
            new() { Icon = "rotate", Title = "Kolay iade" },
            new() { Icon = "shield", Title = "Güvenli ödeme" }
        ];

        public static List<ProductDetailInfoItemDto> FeatureItems() =>
        [
            new() { Icon = "heart", Title = "Elde dikilir", Description = "Her çift, tek tek usta ellerden geçer" },
            new() { Icon = "stretch", Title = "Lastikli ve esnek", Description = "Ayağın rahatça girip çıkmasını sağlar" },
            new() { Icon = "sole", Title = "Yumuşak taban", Description = "İlk adımlara uygun esnek yapı" },
            new() { Icon = "home", Title = "Türkiye'de üretim", Description = "İstanbul'daki atölyemizde hazırlanır" }
        ];
    }
}

public class ProductDetailInfoItemDto
{
    public string Icon { get; set; } = "heart";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
