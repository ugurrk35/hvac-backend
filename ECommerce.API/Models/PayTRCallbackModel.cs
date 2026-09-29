using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Models
{
    public class PayTRCallbackModel
    {
        [FromForm(Name = "merchant_oid")]
        public string merchant_oid { get; set; } = null!;

        [FromForm(Name = "status")]
        public string status { get; set; } = null!;

        [FromForm(Name = "total_amount")]
        public string total_amount { get; set; } = null!;

        [FromForm(Name = "hash")]
        public string hash { get; set; } = null!;

        [FromForm(Name = "payment_id")]
        public string? payment_id { get; set; }

        // İsteğe bağlı alanlar (dilersen ekleyebilirsin)
        [FromForm(Name = "failed_reason_code")]
        public string? failed_reason_code { get; set; }

        [FromForm(Name = "failed_reason_msg")]
        public string? failed_reason_msg { get; set; }
    }

}
