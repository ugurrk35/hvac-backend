using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Repository.Repo
{
    public class ShippingMethodRepository : GenericRepository<ShippingMethod>, IShippingMethodRepository
    {
        public ShippingMethodRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<IReadOnlyList<ShippingMethod>> GetActiveAsync()
            => await _dbContext.ShippingMethods
                .AsNoTracking()
                .Where(method => method.IsActive && !method.IsDeleted)
                .OrderBy(method => method.Price)
                .ThenBy(method => method.Name)
                .ToListAsync();
    }
}
