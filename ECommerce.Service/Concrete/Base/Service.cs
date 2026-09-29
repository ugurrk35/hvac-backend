using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Repo;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete.Base
{
    public class Service<T> : IService<T> where T : BaseEntity
    {
        protected readonly IRepository<T> _repository;
        protected readonly IUnitOfWork _unitOfWork;

        public Service(IRepository<T> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var addedEntity = await _repository.AddAsync(entity);  // repository sadece ekleme yapacak
            await _unitOfWork.CompleteAsync();                        // değişiklikleri burada kaydediyoruz
            return addedEntity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            await _repository.UpdateAsync(entity);                  // repository güncelleme işlemini yapacak
            await _unitOfWork.CommitAsync();                        // commit işlemi
        }

        public virtual async Task DeleteAsync(T entity)
        {
            await _repository.DeleteAsync(entity);
            await _unitOfWork.CommitAsync();
        }

        public virtual async Task SoftDeleteAsync(T entity)
        {
            await _repository.SoftDeleteAsync(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await _repository.ListAsync(spec);
        }

        public async Task<T> GetEntityWithSpecAsync(ISpecification<T> spec)
        {
            return await _repository.GetEntityWithSpecAsync(spec);
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await _repository.CountAsync(spec);
        }
        public  IQueryable<T> Query()
        {
            if (_repository is GenericRepository<T> genericRepo)
            {
                return genericRepo.Query();
            }
            throw new NotSupportedException("Query() metodu sadece GenericRepository ile kullanılabilir.");
        }
        public async Task<int> CountAsync()
        {
            return await _repository.CountAsync();  // Tüm kayıtların sayısını döndürür
        }
    }

}
