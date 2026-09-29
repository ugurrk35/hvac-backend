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
    public class ProductProductTagRepository : GenericRepository<ProductProductTag>, IProductProductTagRepository
    {
        public ProductProductTagRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task RemoveByProductIdAsync(int productId)
        {
            var entities = await _dbContext.ProductProductTags.Where(x => x.ProductId == productId).ToListAsync();
            _dbContext.RemoveRange(entities);
        }
    }
}
