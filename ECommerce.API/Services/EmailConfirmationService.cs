using System.Security.Cryptography;
using System.Text;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public static class EmailConfirmationPurposes
{
    public const string Account = "account-email-verification";
    public const string Marketing = "marketing-email-opt-in";
    public const string Newsletter = "newsletter-email-opt-in";
}

public sealed class EmailConfirmationService
{
    private readonly ApplicationDbContext _db;

    public EmailConfirmationService(ApplicationDbContext db) => _db = db;

    public async Task<string> CreateAsync(string email, string purpose, int? userId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var oldTokens = await _db.EmailConfirmationTokens.Where(item => item.Email == normalizedEmail && item.Purpose == purpose && item.UsedAt == null).ToListAsync(cancellationToken);
        if (oldTokens.Count > 0) _db.EmailConfirmationTokens.RemoveRange(oldTokens);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            UserId = userId,
            Email = normalizedEmail,
            Purpose = purpose,
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<EmailConfirmationToken?> ConsumeAsync(string token, string purpose, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var item = await _db.EmailConfirmationTokens.SingleOrDefaultAsync(value => value.Purpose == purpose && value.TokenHash == Hash(token), cancellationToken);
        if (item == null || item.UsedAt.HasValue || item.ExpiresAt <= DateTime.UtcNow) return null;
        item.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
