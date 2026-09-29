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
    public class UserIdentityRepository : IdentityRepository<ApplicationUser>, IUserIdentityRepository
    {
        public UserIdentityRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<ApplicationUser> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.UserName == name);
        }

        public async Task<ApplicationUser> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
