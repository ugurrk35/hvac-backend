using System;

namespace ECommerce.Domain.Entity
{
    public class MarketingIntegrationSettings : BaseEntity
    {
        public string? GTMContainerId { get; set; }
        public string? GoogleAnalyticsId { get; set; }
        public string? FacebookPixelId { get; set; }
        public string? TikTokPixelId { get; set; }
        public string? MetaConversionApiKey { get; set; }
        public string? MetaConversionApiAccessToken { get; set; }
        public string? GoogleMeasurementProtocolApiSecret { get; set; }
        public string? GoogleAdsConversionId { get; set; }
        public string? TikTokEventsApiAccessToken { get; set; }
        public bool IsMetaServerSideEnabled { get; set; }
        public bool IsGoogleServerSideEnabled { get; set; }
        public bool IsTikTokServerSideEnabled { get; set; }

        public bool IsGTMEnabled { get; set; }
        public bool IsGAEnabled { get; set; }
        public bool IsFacebookEnabled { get; set; }
        public bool IsTikTokEnabled { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
