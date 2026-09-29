using ECommerce.Domain.Entity;

namespace ECommerce.API.Services;

public interface IEmailService
{
    Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(ApplicationUser user, string verificationUrl, CancellationToken cancellationToken = default);
    Task SendMarketingOptInAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default);
    Task<bool> SendContactNotificationAsync(ContactMessage contactMessage, string recipient, CancellationToken cancellationToken = default);
    Task<bool> SendOrderPaidAsync(Order order, CancellationToken cancellationToken = default);
}
