using System;
using System.Collections.Generic;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ExportFilterDto
    {
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public List<int> TagIds { get; set; } = new();
    }
}