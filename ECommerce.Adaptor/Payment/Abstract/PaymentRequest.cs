using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public class PaymentRequest
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }

        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }
        public string CustomerIp { get; set; }

        public string OkUrl { get; set; }
        public string FailUrl { get; set; }

        public List<PaymentBasketItem> Basket { get; set; } = new();
    }
}
