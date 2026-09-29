using ECommerce.Adaptor.Payment.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.PayTR
{
    public class PayTRPaymentRequest : PaymentRequest
    {
        // PayTR’ye özel alanlar
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public int TimeoutLimit { get; set; } = 30; // dakika
        public bool DebugOn { get; set; } = true;
        public string Language { get; set; } = "tr";

        public int NoInstallment { get; set; } = 0;
        public int MaxInstallment { get; set; } = 0;
        public string Currency { get; set; } = "TL";
        public bool? TestMode { get; set; } = null; // null ise config'teki değeri kullan
    }
}
