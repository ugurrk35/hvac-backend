using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IRoleIdentityRepository : IIdentityRepository<ApplicationRole>
    {
        // Gerekirse role özel metotlar eklenebilir
    }
}
