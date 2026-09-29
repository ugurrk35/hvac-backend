using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class ProductAttributeCombinationValueRepository : GenericRepository<ProductAttributeCombinationValue>, IProductAttributeCombinationValueRepository
    {
        public ProductAttributeCombinationValueRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task RemoveByProductIdAsync(int productId)
        {
            var entities = await _dbContext.ProductAttributeCombinationValues.Where(x => x.ProductAttributeCombination.ProductId == productId).ToListAsync();
            _dbContext.RemoveRange(entities);
        }
        public async Task<IEnumerable<ProductAttributeCombinationValue>> GetValuesByCombinationIdAsync(int combinationId)
        {
            return await _dbContext.ProductAttributeCombinationValues
                .Include(cv => cv.ProductAttribute)
                .Include(cv => cv.ProductAttributeValue)
                .Include(cv => cv.ProductAttributeCombination)
                .Where(cv => cv.ProductAttributeCombinationId == combinationId && !cv.IsDeleted)
            .ToListAsync();
        }

        public async Task<ProductAttributeCombinationValue?> GetValueWithDetailsAsync(int id)
        {
            return await _dbContext.ProductAttributeCombinationValues
                .Include(cv => cv.ProductAttribute)
                .Include(cv => cv.ProductAttributeValue)
                .Include(cv => cv.ProductAttributeCombination)
                    .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(cv => cv.Id == id && !cv.IsDeleted);
        }
    }
}
