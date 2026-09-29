namespace ECommerce.API.Dtos.Blog
{
    /// <summary>
    /// Blog yorum oluşturma isteği DTO'su.
    /// Anonim kullanıcılar tarafından gönderilir; moderasyon sonrası yayına alınır.
    /// </summary>
    public class AddBlogCommentDto
    {
        /// <summary>
        /// Yorumun ait olduğu blog yazısının kimliği.
        /// </summary>
        public int BlogId { get; set; }

        /// <summary>
        /// Yorum sahibinin adı (opsiyonel).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Yorum sahibinin e-posta adresi (opsiyonel, gizli tutulur).
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Yorum metni.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Honeypot alanı (bot tespiti için). Boş bırakılmalıdır.
        /// </summary>
        public string? Website { get; set; }
    }
}
