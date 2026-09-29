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
    public class ProductAttributeValueRepository : GenericRepository<ProductAttributeValue>, IProductAttributeValueRepository
    {
        public ProductAttributeValueRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task RemoveByProductIdAsync(int productId)
        {
            var entities = await _dbContext.ProductAttributeValues.Where(x => x.Id == productId).ToListAsync();
            _dbContext.RemoveRange(entities);
        }
        public async Task<IEnumerable<ProductAttributeValue>> GetValuesByAttributeIdAsync(int attributeId)
        {
            return await _dbContext.ProductAttributeValues
                .Include(v => v.ProductAttribute)
                .Where(v => v.ProductAttributeId == attributeId && !v.IsDeleted)
            .ToListAsync();
        }

        public async Task<ProductAttributeValue?> GetValueWithAttributeAsync(int id)
        {
            return await _dbContext.ProductAttributeValues
                .Include(v => v.ProductAttribute)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        }
    }
}
