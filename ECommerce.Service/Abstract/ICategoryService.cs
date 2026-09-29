using ECommerce.Service.Response;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos;
using ECommerce.Service.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface ICategoryService : IService<Category>
    {
        /// <summary>
        /// Slug değerine göre kategoriyi ilgili ürünlerle birlikte getirir
        /// </summary>
        /// <param name="slug">Kategori slug değeri</param>
        /// <returns>Ürünlerle birlikte kategori veya bulunamazsa null</returns>
        Task<Category> GetBySlugAsync(string slug);

        /// <summary>
        /// Tüm aktif kategorileri isme göre sıralı şekilde getirir
        /// </summary>
        /// <returns>Aktif kategorilerin listesi</returns>
        Task<IReadOnlyList<Category>> GetAllActiveAsync();

        /// <summary>
        /// Belirtilen slug değerinin veritabanında mevcut olup olmadığını kontrol eder
        /// </summary>
        /// <param name="slug">Kontrol edilecek slug değeri</param>
        /// <returns>Slug mevcutsa true, değilse false</returns>
        Task<bool> SlugExistsAsync(string slug);

        /// <summary>
        /// ID değerine göre kategoriyi ürünleriyle birlikte getirir
        /// </summary>
        /// <param name="id">Kategori ID değeri</param>
        /// <returns>Ürünlerle birlikte kategori veya bulunamazsa null</returns>
        Task<Category> GetByIdWithProductsAsync(int id);

        /// <summary>
        /// Anahtar kelimeye göre kategori adı veya açıklamasında arama yapar
        /// </summary>
        /// <param name="keyword">Arama anahtar kelimesi</param>
        /// <returns>Eşleşen kategorilerin listesi</returns>
        Task<IReadOnlyList<Category>> SearchAsync(string keyword);

        /// <summary>
        /// Ürün sayısına göre en popüler kategorileri getirir
        /// </summary>
        /// <param name="count">Getirilecek kategori sayısı (varsayılan: 10)</param>
        /// <returns>En popüler kategorilerin listesi</returns>
        Task<IReadOnlyList<Category>> GetTopCategoriesAsync(int count = 10);

        /// <summary>
        /// Validasyon kontrolü ile yeni kategori oluşturur
        /// </summary>
        /// <param name="category">Oluşturulacak kategori</param>
        /// <returns>Oluşturulan kategori</returns>
        Task<Category> CreateCategoryAsync(Category category);

        /// <summary>
        /// Mevcut kategoriyi validasyon kontrolü ile günceller
        /// </summary>
        /// <param name="category">Güncellenecek kategori</param>
        /// <returns>Güncellenmiş kategori</returns>
        Task<Category> UpdateCategoryAsync(Category category);

        /// <summary>
        /// Sadece kategorinin slug değerini günceller
        /// </summary>
        /// <param name="categoryId">Kategori ID değeri</param>
        /// <param name="newSlug">Yeni slug değeri</param>
        /// <returns>Başarılıysa true, değilse false</returns>
        Task<bool> UpdateSlugAsync(int categoryId, string newSlug);

        /// <summary>
        /// Kategoriyi soft delete yöntemiyle siler (IsActive = false)
        /// </summary>
        /// <param name="id">Silinecek kategori ID değeri</param>
        /// <returns>Başarılıysa true, kategori bulunamazsa false</returns>
        Task<bool> DeleteCategoryAsync(int id);

        /// <summary>
        /// Kategori istatistiklerini getirir (toplam sayılar ve analizler)
        /// </summary>
        /// <returns>Kategori istatistikleri nesnesi</returns>
        Task<CategoryStatistics> GetCategoryStatisticsAsync();

        /// <summary>
        /// Kategori verilerini doğrular ve validasyon hatalarının listesini döner
        /// </summary>
        /// <param name="category">Doğrulanacak kategori</param>
        /// <returns>Validasyon hatalarının listesi (geçerliyse boş liste)</returns>
        Task<List<string>> ValidateCategoryAsync(Category category);

        /// <summary>
        /// Kategori adından benzersiz slug oluşturur
        /// </summary>
        /// <param name="name">Kategori adı</param>
        /// <returns>Veritabanında mevcut olmayan benzersiz slug</returns>
        Task<string> GenerateUniqueSlugAsync(string name);

        /// <summary>
        /// Kategorinin güvenle silinip silinemeyeceğini kontrol eder (iş kuralları)
        /// </summary>
        /// <param name="categoryId">Kontrol edilecek kategori ID değeri</param>
        /// <returns>Silinebilirse true, silinememezse false</returns>
        Task<bool> CanDeleteCategoryAsync(int categoryId);

        /// <summary>
        /// Sayfalama desteği ile kategorileri getirir
        /// </summary>
        /// <param name="pageNumber">Sayfa numarası (1 tabanlı)</param>
        /// <param name="pageSize">Sayfa başına öğe sayısı</param>
        /// <param name="searchKeyword">İsteğe bağlı arama anahtar kelimesi</param>
        /// <param name="includeInactive">Pasif kategorileri de dahil et</param>
        /// <returns>Sayfalanmış kategori sonucu</returns>
        Task<PagedResponse<Category>> GetCategoriesPagedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string searchKeyword = null,
            bool includeInactive = false);

        /// <summary>
        /// Kategorileri hiyerarşik yapıda getirir (ebeveyn-çocuk ilişkisi varsa)
        /// </summary>
        /// <returns>Alt kategorileriyle birlikte ana kategorilerin listesi</returns>
        Task<IReadOnlyList<Category>> GetCategoryHierarchyAsync();

        /// <summary>
        /// Kategoriyi aktif veya pasif duruma getirir
        /// </summary>
        /// <param name="categoryId">Kategori ID değeri</param>
        /// <param name="isActive">Aktif durumu</param>
        /// <returns>Başarılıysa true, kategori bulunamazsa false</returns>
        Task<bool> SetCategoryStatusAsync(int categoryId, bool isActive);

        /// <summary>
        /// Birden fazla kritere uyan kategorileri getirir
        /// </summary>
        /// <param name="filter">Kategori filtreleme kriterleri</param>
        /// <returns>Eşleşen kategorilerin listesi</returns>
        //Task<IReadOnlyList<Category>> GetCategoriesByFilterAsync(CategoryFilter filter);

        ///// <summary>
        ///// Kategoriler üzerinde toplu işlem yapar
        ///// </summary>
        ///// <param name="categoryIds">Kategori ID listesi</param>
        ///// <param name="operation">Yapılacak işlem türü</param>
        ///// <returns>Etkilenen kategori sayısı</returns>
        //Task<int> BulkUpdateCategoriesAsync(List<int> categoryIds, BulkCategoryOperation operation);
    }
}

