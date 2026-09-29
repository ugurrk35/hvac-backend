using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.PayTR
{
    public class PayTRConfig
    {
        public string MerchantId { get; set; }
        public string MerchantKey { get; set; }
        public string MerchantSalt { get; set; }

        public string ApiUrl { get; set; } = "https://www.paytr.com/odeme/api/get-token";
        public string IFrameBaseUrl { get; set; } = "https://www.paytr.com/odeme/guvenli/";
        public string RefundApiUrl { get; set; } = "https://www.paytr.com/odeme/iade";

        public int NoInstallment { get; set; } = 0;
        public int MaxInstallment { get; set; } = 0;
        public string Currency { get; set; } = "TL";
        public bool TestMode { get; set; } = true;
        public int TimeoutLimit { get; set; } = 30;
        public bool DebugOn { get; set; } = true;
        public string Language { get; set; } = "tr";
    }

}
