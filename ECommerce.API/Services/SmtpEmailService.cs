using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using ECommerce.Domain.Entity;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger, ApplicationDbContext db, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;
        var name = WebUtility.HtmlEncode(string.Join(" ", new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))));
        var template = await GetTemplateAsync("welcome", "Kombi Klima Burada'ya hoş geldiniz", "<p>Merhaba {customerName},</p><p>Kombi Klima Burada hesabınız başarıyla oluşturuldu.</p>", cancellationToken);
        if (template == null) return;
        await SendAsync(user.Email, Replace(template.Value.Subject, ("customerName", name)), Replace(template.Value.Body, ("customerName", name)), cancellationToken);
    }

    public async Task SendEmailVerificationAsync(ApplicationUser user, string verificationUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;
        var name = WebUtility.HtmlEncode(string.Join(" ", new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))));
        var template = await GetTemplateAsync("email-verification", "E-posta adresinizi doğrulayın", "<p>Merhaba {customerName},</p><p>Kombi Klima Burada hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın.</p><p><a href=\"{verificationUrl}\">E-posta adresimi doğrula</a></p><p>Bu bağlantı 24 saat geçerlidir.</p>", cancellationToken);
        if (template == null) return;
        var values = new[] { ("customerName", name), ("verificationUrl", WebUtility.HtmlEncode(verificationUrl)) };
        await SendAsync(user.Email, Replace(template.Value.Subject, values), Replace(template.Value.Body, values), cancellationToken);
    }

    public async Task SendMarketingOptInAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync("marketing-opt-in", "Bülten aboneliğinizi onaylayın", "<p>Merhaba,</p><p>Kampanya ve ürün haberlerini almak için e-posta adresinizi onaylayın.</p><p><a href=\"{confirmationUrl}\">Bülten aboneliğini onayla</a></p><p>Bu bağlantı 24 saat geçerlidir. Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>", cancellationToken);
        if (template == null) return;
        var values = new[] { ("confirmationUrl", WebUtility.HtmlEncode(confirmationUrl)) };
        await SendAsync(email, Replace(template.Value.Subject, values), Replace(template.Value.Body, values), cancellationToken);
    }

    public async Task<bool> SendContactNotificationAsync(ContactMessage contactMessage, string recipient, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient)) return false;

        var template = await GetTemplateAsync(
            "contact-notification",
            "Yeni iletişim mesajı · {contactSubject}",
            "<p>Web sitenizden yeni bir iletişim mesajı geldi.</p><p><strong>Ad Soyad:</strong> {contactName}<br/><strong>E-posta:</strong> {contactEmail}<br/><strong>Konu:</strong> {contactSubject}</p><p><strong>Mesaj:</strong></p><p>{contactMessage}</p>",
            cancellationToken);
        if (template == null) return false;

        var values = new[]
        {
            ("contactName", WebUtility.HtmlEncode(contactMessage.Name)),
            ("contactEmail", WebUtility.HtmlEncode(contactMessage.Email)),
            ("contactSubject", WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(contactMessage.Subject) ? "Konu belirtilmedi" : contactMessage.Subject)),
            ("contactMessage", WebUtility.HtmlEncode(contactMessage.Message).Replace("\r\n", "<br/>").Replace("\n", "<br/>"))
        };
        return await SendAsync(recipient, Replace(template.Value.Subject, values), Replace(template.Value.Body, values), cancellationToken);
    }

    public async Task<bool> SendOrderPaidAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerEmail)) return false;

        var customerName = WebUtility.HtmlEncode(string.Join(" ", new[] { order.CustomerFirstName, order.CustomerLastName }.Where(value => !string.IsNullOrWhiteSpace(value))));
        var orderNumber = WebUtility.HtmlEncode(order.OrderNumber ?? order.Id.ToString(CultureInfo.InvariantCulture));
        var total = order.TotalAmount.ToString("C", CultureInfo.GetCultureInfo("tr-TR"));
        var items = order.OrdersItems?.Where(item => !item.IsGift).Select(item =>
            $"<li>{WebUtility.HtmlEncode(item.ProductName ?? item.Product?.Name ?? "Ürün")} × {item.Quantity}</li>") ?? [];

        var template = await GetTemplateAsync("order-paid", "Siparişiniz alındı · #{orderNumber}", "<p>Merhaba {customerName},</p><p><strong>#{orderNumber}</strong> numaralı siparişinizin ödemesi başarıyla alındı.</p><ul>{orderItems}</ul><p><strong>Toplam: {orderTotal}</strong></p>", cancellationToken);
        if (template == null)
        {
            _logger.LogWarning("Ödeme e-postası gönderilmedi çünkü 'order-paid' e-posta şablonu pasif.");
            return false;
        }
        var values = new[] { ("customerName", customerName), ("orderNumber", orderNumber), ("orderItems", string.Join(string.Empty, items)), ("orderTotal", total) };
        return await SendAsync(order.CustomerEmail, Replace(template.Value.Subject, values), Replace(template.Value.Body, values), cancellationToken);
    }

    private async Task<bool> SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogError("E-posta gönderimi için SMTP yapılandırması eksik. Email__Enabled, Email__Host, Email__User ve Email__Password değerlerini kontrol edin.");
            return false;
        }

        try
        {
            if (_options.UsesResendApi)
            {
                return await SendWithResendApiAsync(recipient, subject, htmlBody, cancellationToken);
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.SenderAddress));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = HtmlToText(htmlBody) }.ToMessageBody();

            using var smtp = new SmtpClient();
            if (_options.AllowInvalidCertificate)
            {
                // Stalwart uses a self-signed certificate on its private Docker
                // hostname. Public SMTP hosts must leave this disabled.
                smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60)));
            await smtp.ConnectAsync(_options.Host, _options.Port,
                _options.UseSslOnConnect ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                timeout.Token);
            await smtp.AuthenticateAsync(_options.User, _options.Password, timeout.Token);
            await smtp.SendAsync(message, timeout.Token);
            await smtp.DisconnectAsync(true, timeout.Token);
            _logger.LogInformation("E-posta başarıyla gönderildi. Alıcı: {Recipient}, Konu: {Subject}", recipient, subject);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("SMTP sunucusu {TimeoutSeconds} saniye içinde yanıt vermedi. Host: {Host}, Port: {Port}", Math.Clamp(_options.TimeoutSeconds, 5, 60), _options.Host, _options.Port);
            return false;
        }
        catch (Exception exception)
        {
            // E-posta altyapısı ödeme/kayıt akışını kesintiye uğratmamalıdır.
            _logger.LogError(exception, "E-posta gönderilemedi. Alıcı: {Recipient}, Konu: {Subject}", recipient, subject);
            return false;
        }
    }

    private async Task<bool> SendWithResendApiAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Password);
        request.Content = JsonContent.Create(new
        {
            from = new MailboxAddress(_options.FromName, _options.SenderAddress).ToString(),
            to = new[] { recipient },
            subject,
            html = htmlBody,
            text = HtmlToText(htmlBody)
        });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60)));
        using var response = await client.SendAsync(request, timeout.Token);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Resend API ile e-posta başarıyla gönderildi. Alıcı: {Recipient}, Konu: {Subject}", recipient, subject);
            return true;
        }

        var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
        _logger.LogError("Resend API e-posta gönderimini reddetti. Durum: {StatusCode}, Yanıt: {Response}", response.StatusCode, responseBody);
        return false;
    }

    private static string HtmlToText(string html) => System.Text.RegularExpressions.Regex
        .Replace(html, "<[^>]+>", " ")
        .Replace("&nbsp;", " ")
        .Trim();

    private async Task<(string Subject, string Body)?> GetTemplateAsync(string key, string defaultSubject, string defaultBody, CancellationToken cancellationToken)
    {
        var saved = await _db.EmailTemplates.AsNoTracking().SingleOrDefaultAsync(item => item.TemplateKey == key, cancellationToken);
        return saved is { IsActive: false } ? null : (saved?.Subject ?? defaultSubject, saved?.HtmlBody ?? defaultBody);
    }

    private static string Replace(string value, params (string Key, string Value)[] values) => values.Aggregate(value, (current, item) => current.Replace($"{{{item.Key}}}", item.Value, StringComparison.Ordinal));
}
