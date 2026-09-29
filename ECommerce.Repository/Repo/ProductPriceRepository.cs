using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class ProductPriceRepository : GenericRepository<ProductPrice>, IProductPriceRepository
    {
        public ProductPriceRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ProductPrice> GetPriceAsync(int productId, int? productAttributeCombinationId = null, int? customerGroupId = null)
        {
            var query = _dbContext.Set<ProductPrice>().AsQueryable();
            query = query.Where(p => p.ProductId == productId);
            if (productAttributeCombinationId.HasValue)
                query = query.Where(p => p.ProductAttributeCombinationId == productAttributeCombinationId);
            if (customerGroupId.HasValue)
                query = query.Where(p => p.CustomerGroupId == customerGroupId);
            return await query.FirstOrDefaultAsync();
        }
        public async Task<ProductPrice> GetLatestPriceAsync(int productId, int? productAttributeCombinationId = null, int? customerGroupId = null)
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Set<ProductPrice>()
                .Where(p => p.ProductId == productId
                            && (productAttributeCombinationId == null || p.ProductAttributeCombinationId == productAttributeCombinationId)
                            && (customerGroupId == null || p.CustomerGroupId == customerGroupId)
                            && (p.EndDate == null || p.EndDate >= now))
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Product>> GetProductsByIdsOrCategoryAsync(List<int> productIds, int? categoryId)
        {
            var query = _dbContext.Products.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (productIds?.Any() == true)
                query = query.Where(p => productIds.Contains(p.Id));

            return await query
                .Include(p => p.ProductAttributeCombination)
                    .ThenInclude(c => c.ProductAttributeCombinationValues)
                .ToListAsync();
        }

        public async Task RemoveByProductIdAsync(int productId)
        {
            var prices = _dbContext.Set<ProductPrice>().Where(p => p.ProductId == productId);
            _dbContext.Set<ProductPrice>().RemoveRange(prices);
            await _dbContext.SaveChangesAsync();
        }
        


    }
}
