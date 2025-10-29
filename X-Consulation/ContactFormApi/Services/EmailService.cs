using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using X_Consulation.ContactFormApi.Models;

namespace X_Consulation.ContactFormApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendContactFormEmailAsync(ContactForm contactForm)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];
            var adminEmail = smtpSettings["AdminEmail"];
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("Admin", adminEmail));
            message.Subject = $"Yeni İletişim Formu - {contactForm.Topic}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = GenerateContactFormEmailBody(contactForm)
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendEmailAsync(message, smtpSettings);

            _logger.LogInformation($"İletişim formu e-postası başarıyla gönderildi. Form ID: {contactForm.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"İletişim formu e-postası gönderirken hata oluştu: {ex.Message}");
            throw;
        }
    }

    public async Task SendConfirmationEmailAsync(string recipientEmail, string recipientName, string language)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = GetConfirmationSubject(language);

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = GenerateConfirmationEmailBody(recipientName, language)
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendEmailAsync(message, smtpSettings);

            _logger.LogInformation($"Onay e-postası {recipientEmail} adresine gönderildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Onay e-postası gönderirken hata oluştu: {ex.Message}");
            throw;
        }
    }

    private async Task SendEmailAsync(MimeMessage message, IConfigurationSection smtpSettings)
    {
        var smtpServer = smtpSettings["SmtpServer"];
        var smtpPort = int.Parse(smtpSettings["SmtpPort"]);
        var smtpUsername = smtpSettings["SmtpUsername"];
        var smtpPassword = smtpSettings["SmtpPassword"];
        var useStartTls = bool.Parse(smtpSettings["UseStartTls"] ?? "true");

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpServer, smtpPort, 
                useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            
            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }

    private string GenerateContactFormEmailBody(ContactForm contactForm)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; border-radius: 3px; }}
                    .content {{ padding: 20px 0; }}
                    .field {{ margin-bottom: 15px; }}
                    .label {{ font-weight: bold; color: #4CAF50; }}
                    .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Yeni İletişim Formu Başvurusu</h2>
                    </div>
                    <div class='content'>
                        <div class='field'>
                            <span class='label'>Ad Soyad:</span> {contactForm.Name}
                        </div>
                        <div class='field'>
                            <span class='label'>E-posta:</span> {contactForm.Email}
                        </div>
                        <div class='field'>
                            <span class='label'>Telefon:</span> {contactForm.PhoneNumber}
                        </div>
                        <div class='field'>
                            <span class='label'>Konu:</span> {contactForm.Topic}
                        </div>
                        <div class='field'>
                            <span class='label'>Mesaj:</span><br/>
                            {contactForm.Message.Replace("\n", "<br/>")}
                        </div>
                        <div class='field'>
                            <span class='label'>Gönderim Tarihi:</span> {contactForm.SubmittedAt:dd.MM.yyyy HH:mm:ss}
                        </div>
                        <div class='field'>
                            <span class='label'>Dil:</span> {GetLanguageName(contactForm.Language)}
                        </div>
                    </div>
                    <div class='footer'>
                        <p>Bu e-posta otomatik olarak gönderilmiştir. Lütfen yanıt vermeyin.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateConfirmationEmailBody(string recipientName, string language)
    {
        var (title, greeting, message, thanks, footer) = GetConfirmationTexts(language);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; border-radius: 3px; }}
                    .content {{ padding: 20px 0; }}
                    .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>{title}</h2>
                    </div>
                    <div class='content'>
                        <p>{greeting} {recipientName},</p>
                        <p>{message}</p>
                        <p>{thanks}</p>
                    </div>
                    <div class='footer'>
                        <p>{footer}</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GetConfirmationSubject(string language)
    {
        return language switch
        {
            "en" => "Your Form Has Been Received",
            "fr" => "Votre formulaire a été reçu",
            "ku" => "Forma we ya we hat gihişt",
            _ => "İletişim Formunuz Alındı"
        };
    }

    private (string title, string greeting, string message, string thanks, string footer) GetConfirmationTexts(string language)
    {
        return language switch
        {
            "en" => (
                "Form Received",
                "Hello",
                "Your contact form has been successfully received. We will get back to you as soon as possible.",
                "Thank you!",
                "X Consultation - Contact System"
            ),
            "fr" => (
                "Formulaire Reçu",
                "Bonjour",
                "Votre formulaire de contact a été reçu avec succès. Nous vous répondrons dès que possible.",
                "Merci!",
                "X Consultation - Système de Contact"
            ),
            "ku" => (
                "Forma Hate",
                "Bi xêr",
                "Forma kontakta we bi serkeftî hate qebûl kirin. Em lez re dê bi we re têkevin.",
                "Spas!",
                "X Consultation - Pergala Têkiliyê"
            ),
            _ => (
                "Formunuz Alındı",
                "Merhaba",
                "İletişim formunuz başarıyla alınmıştır. En kısa zamanda sizinle iletişime geçeceğiz.",
                "Teşekkürler!",
                "X Consultation - İletişim Sistemi"
            )
        };
    }

    private string GetLanguageName(string language)
    {
        return language switch
        {
            "en" => "English",
            "fr" => "Français",
            "ku" => "Kurdî",
            _ => "Türkçe"
        };
    }
}