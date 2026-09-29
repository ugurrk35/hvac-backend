using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public class PaymentInitializeResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string IFrameUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
}
