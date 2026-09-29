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
    public class SessionRepository : GenericRepository<Session>, ISessionRepository
    {
        public SessionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Session?> GetByTokenAsync(string token)
        {
            return await _dbContext.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Token == token);
        }
    }
}
