using System;

namespace ECommerce.Domain.Entity
{
    public class WhatsAppSettings : BaseEntity
    {
        public string? PhoneNumber { get; set; }
        public bool IsEnabled { get; set; }
        public string? ProductTemplate { get; set; }
        public string? CartTemplate { get; set; }
        public string? CheckoutTemplate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
