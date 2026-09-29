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
    public class ProductAttributeCombinationRepository : GenericRepository<ProductAttributeCombination>, IProductAttributeCombinationRepository
    {
        public ProductAttributeCombinationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ProductAttributeCombination>> GetCombinationsByProductIdAsync(int productId)
        {
            return await _dbContext.ProductAttributeCombinations
                .Include(c => c.Product)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttribute)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttributeValue)
                .Where(c => c.ProductId == productId && !c.IsDeleted)
            .ToListAsync();
        }
        public async Task RemoveByProductIdAsync(int productId)
        {
            var entities = await _dbContext.ProductAttributeCombinations.Where(x => x.ProductId == productId).ToListAsync();
            _dbContext.RemoveRange(entities);
        }
        public async Task<ProductAttributeCombination?> GetCombinationWithValuesAsync(int id)
        {
            return await _dbContext.ProductAttributeCombinations
                .Include(c => c.Product)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttribute)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttributeValue)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<ProductAttributeCombination?> GetCombinationBySkuAsync(string sku)
        {
            return await _dbContext.ProductAttributeCombinations
                .Include(c => c.Product)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttribute)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttributeValue)
                .FirstOrDefaultAsync(c => c.Sku == sku && !c.IsDeleted);
        }

        public async Task<IEnumerable<ProductAttributeCombination>> GetCombinationsWithValuesAsync()
        {
            return await _dbContext.ProductAttributeCombinations
                .Include(c => c.Product)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttribute)
                .Include(c => c.ProductAttributeCombinationValues)
                    .ThenInclude(cv => cv.ProductAttributeValue)
                .Where(c => !c.IsDeleted)
                .ToListAsync();
        }
    }
}
