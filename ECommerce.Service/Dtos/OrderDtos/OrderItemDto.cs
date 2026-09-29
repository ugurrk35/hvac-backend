using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductImageUrl { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ListUnitPrice { get; set; }
        public decimal ProductDiscountTotal { get; set; }
        public string? VariantSnapshot { get; set; }
        public int? ProductCampaignPackageId { get; set; }
        public string? CampaignSnapshotJson { get; set; }
    }
}
