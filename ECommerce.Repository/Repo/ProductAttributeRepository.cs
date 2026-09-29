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
    public class ProductAttributeRepository : GenericRepository<ProductAttribute>, IProductAttributeRepository
    {
        public ProductAttributeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ProductAttribute>> GetAttributesWithValuesAsync()
        {
            return await _dbContext.ProductAttributes
                .Include(a => a.ProductAttributeValues)
                .Where(a => !a.IsDeleted)
            .ToListAsync();
        }

        public async Task<ProductAttribute?> GetAttributeWithValuesByIdAsync(int id)
        {
            return await _dbContext.ProductAttributes
                .Include(a => a.ProductAttributeValues.Where(v => !v.IsDeleted))
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<IEnumerable<ProductAttribute>> GetPersonalizationAttributesAsync()
        {
            return await _dbContext.ProductAttributes
                .Where(a => a.IsPersonalizationText && !a.IsDeleted)
                .ToListAsync();
        }
    }
}
