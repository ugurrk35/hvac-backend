using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductAttributeDtos
{
    public class LookupPersonalizationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsPersonalization { get; set; }
    }
}
