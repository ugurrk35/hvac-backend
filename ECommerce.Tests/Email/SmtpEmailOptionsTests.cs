using ECommerce.API.Services;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Email;

public class SmtpEmailOptionsTests
{
    [Fact]
    public void SenderAddress_UsesAuthenticatedMailbox_WhenFromAddressIsMissing()
    {
        var options = new SmtpEmailOptions
        {
            Enabled = true,
            Host = "mail.kombiklimaburada.com",
            User = "info@kombiklimaburada.com",
            Password = "test-password"
        };

        options.IsConfigured.Should().BeTrue();
        options.SenderAddress.Should().Be("info@kombiklimaburada.com");
    }

    [Fact]
    public void SenderAddress_PrefersExplicitFromAddress()
    {
        var options = new SmtpEmailOptions
        {
            User = "smtp@kombiklimaburada.com",
            FromAddress = "info@kombiklimaburada.com"
        };

        options.SenderAddress.Should().Be("info@kombiklimaburada.com");
    }

    [Fact]
    public void AllowInvalidCertificate_IsDisabledByDefault_AndCanBeExplicitlyEnabledForPrivateSmtp()
    {
        new SmtpEmailOptions().AllowInvalidCertificate.Should().BeFalse();

        var options = new SmtpEmailOptions { AllowInvalidCertificate = true };
        options.AllowInvalidCertificate.Should().BeTrue();
    }
}
