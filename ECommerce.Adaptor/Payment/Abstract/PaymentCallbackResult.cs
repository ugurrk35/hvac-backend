using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public class PaymentCallbackResult
    {
        public bool Success { get; set; }
        public string OrderId { get; set; }
        public decimal PaidAmount { get; set; }
        public string ErrorMessage { get; set; }
    }
}
