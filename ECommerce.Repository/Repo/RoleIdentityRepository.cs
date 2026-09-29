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
    public class RoleIdentityRepository : IdentityRepository<ApplicationRole>, IRoleIdentityRepository
    {
        public RoleIdentityRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<ApplicationRole> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
        }
    }
}
