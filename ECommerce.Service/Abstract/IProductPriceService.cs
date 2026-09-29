using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.ProductDtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductPriceService
    {
        Task BulkUpsertProductPricesAsync(BulkProductPriceDto dto);
        Task<ProductPriceDto> GetByIdAsync(int id);
        Task<IReadOnlyList<ProductPriceDto>> GetAllAsync();
        Task<ProductPriceDto> CreateAsync(ProductPriceDto dto);
        Task UpdateAsync(int id, ProductPriceDto dto);
        Task DeleteAsync(int id);
        Task<(IReadOnlyList<ProductPriceDto> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ProductPriceFilterDto filter);
    }

    public class ProductPriceFilterDto
    {
        public int? ProductId { get; set; }
        public int? ProductAttributeCombinationId { get; set; }
        public int? CustomerGroupId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
