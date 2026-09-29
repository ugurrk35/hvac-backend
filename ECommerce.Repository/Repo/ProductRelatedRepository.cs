using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class ProductRelatedRepository : GenericRepository<ProductRelated>, IProductRelatedRepository
    {
        public ProductRelatedRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task RemoveByProductIdAsync(int productId)
        {
            var items = await _dbContext.ProductRelateds
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            if (items.Any())
            {
                _dbContext.ProductRelateds.RemoveRange(items);
            }
        }

        public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int count = 4, bool? forCart = null)
        {
            var query = from pr in _dbContext.ProductRelateds
                        join p in _dbContext.Products on pr.RelatedProductId equals p.Id
                        where pr.ProductId == productId && p.IsActive && p.IsPublished && !p.IsDeleted && p.Quantity > 0
                            && (!forCart.HasValue || (forCart.Value ? pr.ShowInCart : pr.ShowOnProductPage))
                        orderby pr.SortOrder, p.Id
                        select p;

            return await query
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.Category)
                .Take(count)
                .ToListAsync();
        }
    }
}
