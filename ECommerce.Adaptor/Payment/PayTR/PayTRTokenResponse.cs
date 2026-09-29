using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.PayTR
{
    public class PayTRTokenResponse
    {
        public string status { get; set; }
        public string token { get; set; }
        public string reason { get; set; }
    }
}
