using X_Consulation.ContactFormApi.Models;

namespace X_Consulation.ContactFormApi.Services;

public interface IEmailService
{
    Task SendContactFormEmailAsync(ContactForm contactForm);
    Task SendConfirmationEmailAsync(string recipientEmail, string recipientName, string language);
}

