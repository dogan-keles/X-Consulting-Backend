using X_Consulation.ContactFormApi.Models;

namespace X_Consulation.ContactFormApi.Services;

public interface IEmailService
{
    Task SendContactFormEmailAsync(ContactForm contactForm);
    Task SendConfirmationEmailAsync(string recipientEmail, string recipientName, string language);
    Task SendQuickAppointmentEmailAsync(QuickAppointment appointment);
    Task SendAppointmentConfirmationEmailAsync(string phoneNumber, string name, string language);
    Task SendDateTimeUpdateEmailAsync(string phoneNumber, string name, string preferredDate, string preferredTime, string language);


}

