using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class Address: AuditableEntity
    {

        public string? Country { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Neighborhood { get; set; }
        public string? AddressLine { get; set; }
        public string? PostalCode { get; set; }
    }
}
