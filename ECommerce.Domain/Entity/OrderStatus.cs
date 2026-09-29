using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public enum OrderStatus
    {
        Pending=0,
        Processing=1,
        Shipped=2,
        Delivered=3,
        Canceled=4,
        Completed= 5,
        Returned = 6
    }
}
