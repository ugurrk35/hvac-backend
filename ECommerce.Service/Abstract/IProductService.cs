using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Dtos.ProductDtos.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductService : IService<Product>
    {
        Task<IReadOnlyList<Product>> SearchByNameAsync(string searchTerm);
        Task<IReadOnlyList<Product>> GetByCategoryIdAsync(int categoryId);
        Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<IReadOnlyList<Product>> GetInStockProductsAsync();
        Task<IReadOnlyList<Product>> GetPopularProductsAsync(int topCount);
        Task<Product> GetProductDetailsAsync(int productId);
        Task UpdateStockAsync(int productId, int quantityChange);
        Task<IReadOnlyList<Product>> GetFilteredProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, bool? inStock, string searchTerm);
        Task<Product> GetBySlugAsync(string slug);
        Task<bool> IsSlugUniqueAsync(string slug, int? productIdToExclude = null);


        Task<List<ProductResponseCollectionDto>> GetCollectionProductsAsync(string collection, string currency);
        Task<(IReadOnlyList<Product> Products, int TotalCount)> GetPagedProductsAsync(ProductFilterDto filter);
        Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int count = 5, bool? forCart = null);
        Task<IReadOnlyList<Product>> GetProductsByTagsAsync(List<int> tagIds);
        Task<Product> CreateProductWithRelationsAsync(CreateProductDto createDto);
        Task<Product> UpdateProductWithRelationsAsync(UpdateProductDto updateDto);
        Task BulkUpdatePricesAsync(List<ProductPriceUpdateDto> priceUpdates);
        Task<bool> IsSKUUniqueAsync(string sku, int? productIdToExclude = null);
        Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count = 10);
        Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int count = 10);
        Task<IReadOnlyList<Product>> GetDiscountedProductsAsync();
        Task SetFeaturedStatusAsync(int productId, bool isFeatured);
        Task<Dictionary<string, int>> GetProductStatisticsAsync();
        Task BulkSoftDeleteAsync(List<int> productIds);
        Task BulkUpdatePublishedStatusAsync(List<int> productIds, bool isPublished);
        Task<byte[]> ExportProductsAsync(ExportFilterDto filter);
    }
}
