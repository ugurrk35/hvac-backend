using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<int> CountAsync();
        // Temel CRUD
        Task<T> GetByIdAsync(int id);
        Task<IReadOnlyList<T>> GetAllAsync();

        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);  // Fiziksel silme
        Task SoftDeleteAsync(T entity); // Soft delete (IsActive false gibi)

        // Specification Pattern ile filtreleme
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
        Task<T> GetEntityWithSpecAsync(ISpecification<T> spec);
        Task<int> CountAsync(ISpecification<T> spec);

        // Queryable erişim (Gelişmiş sorgular için)
        IQueryable<T> Query();
    }
}
