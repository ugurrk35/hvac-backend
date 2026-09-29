using ECommerce.Domain.Entity;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<List<Product>> GetProductsByIdsOrCategoryAsync(List<int> productIds, int? categoryId);
        Task<IReadOnlyList<Product>> SearchByNameAsync(string searchTerm);

        Task<IReadOnlyList<Product>> GetByCategoryIdAsync(int categoryId);

        Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<Product?> GetByIdForUpdateAsync(int id); // WITH (UPDLOCK, ROWLOCK)
        Task<IReadOnlyList<Product>> GetInStockProductsAsync();
        Task<IEnumerable<Product>> GetAllByIdsAsync(List<int> ids);
        Task<IReadOnlyList<Product>> GetPopularProductsAsync(int topCount);
        Task<List<Product>> GetPublishedProductsByCollectionAsync(string collection);

        Task<Product> GetProductDetailsAsync(int productId);

        Task UpdateStockAsync(int productId, int quantityChange);
        // Slug ile ürün getirme
        Task<Product> GetBySlugAsync(string slug);
        Task DeactivateByProductIdAsync(int productId);
        

        // Slug var mı kontrol etme (örn. yeni ürün eklerken benzersiz kontrol için)
        Task<bool> IsSlugUniqueAsync(string slug, int? productIdToExclude = null);
        Task<IReadOnlyList<Product>> GetFilteredProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, bool? inStock, string searchTerm);
    }
}