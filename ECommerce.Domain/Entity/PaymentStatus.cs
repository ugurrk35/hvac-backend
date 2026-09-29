using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public enum PaymentStatus
    {
        Unpaid = 0,
        Success = 1,
        Failed = 2,
        Refunded = 3,
        Cancelled = 4,
        Paid=5


    }
}
