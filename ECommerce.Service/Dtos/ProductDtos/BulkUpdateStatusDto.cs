using System.Collections.Generic;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class BulkUpdateStatusDto
    {
        public List<int> ProductIds { get; set; } = new();
        public string NewStatus { get; set; } = string.Empty;
    }
}