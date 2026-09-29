using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public class PaymentBasketItem
    {
        public string Name { get; set; }
        public decimal Total { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
