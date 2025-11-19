using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using X_Consulation.ContactFormApi.Models;

namespace X_Consulation.ContactFormApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendContactFormEmailAsync(ContactForm contactForm)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];
            var adminEmail = smtpSettings["AdminEmail"];
            
            var subject = $"Yeni İletişim Formu - {contactForm.Topic}";
            var htmlBody = GenerateContactFormEmailBody(contactForm);

            await SendEmailViaResendAsync(senderEmail, senderName, adminEmail, "Admin", subject, htmlBody);

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

            var subject = GetConfirmationSubject(language);
            var htmlBody = GenerateConfirmationEmailBody(recipientName, language);

            await SendEmailViaResendAsync(senderEmail, senderName, recipientEmail, recipientName, subject, htmlBody);

            _logger.LogInformation($"Onay e-postası {recipientEmail} adresine gönderildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Onay e-postası gönderirken hata oluştu: {ex.Message}");
            throw;
        }
    }

    public async Task SendQuickAppointmentEmailAsync(QuickAppointment appointment)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];
            var adminEmail = smtpSettings["AdminEmail"];
            
            var subject = $"🚀 Yeni Hızlı Randevu Talebi - {appointment.Name}";
            var htmlBody = GenerateQuickAppointmentEmailBody(appointment);

            await SendEmailViaResendAsync(senderEmail, senderName, adminEmail, "Admin", subject, htmlBody);

            _logger.LogInformation($"Hızlı randevu e-postası başarıyla gönderildi. Randevu ID: {appointment.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Hızlı randevu e-postası gönderirken hata oluştu: {ex.Message}");
            throw;
        }
    }

    public async Task SendAppointmentConfirmationEmailAsync(string phoneNumber, string name, string language)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];

            var subject = GetAppointmentConfirmationSubject(language);
            var htmlBody = GenerateAppointmentConfirmationEmailBody(name, phoneNumber, language);

            await SendEmailViaResendAsync(senderEmail, senderName, senderEmail, senderName, subject, htmlBody);

            _logger.LogInformation($"Randevu onay e-postası gönderildi. Telefon: {phoneNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Randevu onay e-postası gönderirken hata oluştu: {ex.Message}");
            throw;
        }
    }

    public async Task SendDateTimeUpdateEmailAsync(string phoneNumber, string name, string preferredDate, string preferredTime, string language)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];
            var adminEmail = smtpSettings["AdminEmail"];
            
            var subject = $"📅 Randevu Tarih/Saat Tercihi - {name}";
            var htmlBody = GenerateDateTimeUpdateEmailBody(phoneNumber, name, preferredDate, preferredTime, language);

            await SendEmailViaResendAsync(senderEmail, senderName, adminEmail, "Admin", subject, htmlBody);

            _logger.LogInformation($"Tarih/saat güncelleme e-postası gönderildi. Telefon: {phoneNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Tarih/saat güncelleme e-postası gönderirken hata: {ex.Message}");
            throw;
        }
    }

    private async Task SendEmailViaResendAsync(string fromEmail, string fromName, string toEmail, string toName, string subject, string htmlBody)
    {
        var apiKey = _configuration["ResendApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("ResendApiKey configuration is missing");
        }

        var requestBody = new
        {
            from = $"{fromName} <onboarding@resend.dev>",
            to = new[] { toEmail },
            subject = subject,
            html = htmlBody
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Resend API error: {response.StatusCode} - {errorContent}");
            throw new Exception($"Failed to send email via Resend: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Email sent successfully via Resend. Response: {responseContent}");
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

    private string GenerateQuickAppointmentEmailBody(QuickAppointment appointment)
    {
        var dateTimeInfo = "";
        if (!string.IsNullOrEmpty(appointment.PreferredDate))
        {
            dateTimeInfo = $@"
                <div class='field'>
                    <span class='label'>Tercih Edilen Tarih:</span> {appointment.PreferredDate}
                </div>
                <div class='field'>
                    <span class='label'>Tercih Edilen Saat:</span> {appointment.PreferredTime ?? "Belirtilmedi"}
                </div>";
        }

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px; border-radius: 3px; }}
                    .content {{ padding: 20px 0; }}
                    .field {{ margin-bottom: 15px; }}
                    .label {{ font-weight: bold; color: #667eea; }}
                    .badge {{ display: inline-block; padding: 5px 10px; background-color: #4CAF50; color: white; border-radius: 3px; font-size: 12px; }}
                    .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>🚀 Yeni Hızlı Randevu Talebi</h2>
                    </div>
                    <div class='content'>
                        <div class='field'>
                            <span class='badge'>YENİ</span>
                        </div>
                        <div class='field'>
                            <span class='label'>Ad Soyad:</span> {appointment.Name}
                        </div>
                        <div class='field'>
                            <span class='label'>Telefon:</span> {appointment.PhoneNumber}
                        </div>
                        <div class='field'>
                            <span class='label'>Mesaj:</span><br/>
                            {appointment.Message.Replace("\n", "<br/>")}
                        </div>
                        {dateTimeInfo}
                        <div class='field'>
                            <span class='label'>Gönderim Tarihi:</span> {appointment.SubmittedAt:dd.MM.yyyy HH:mm:ss}
                        </div>
                        <div class='field'>
                            <span class='label'>Dil:</span> {GetLanguageName(appointment.Language)}
                        </div>
                        <div class='field'>
                            <span class='label'>Durum:</span> <span style='color: #FF9800;'>Beklemede</span>
                        </div>
                    </div>
                    <div class='footer'>
                        <p>⚡ Bu randevu talebi hızlı randevu sistemi üzerinden gelmiştir.</p>
                        <p>Lütfen en kısa sürede müşteri ile iletişime geçin.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateAppointmentConfirmationEmailBody(string name, string phoneNumber, string language)
    {
        var (title, greeting, message, thanks, footer) = GetAppointmentConfirmationTexts(language);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px; border-radius: 3px; }}
                    .content {{ padding: 20px 0; }}
                    .icon {{ font-size: 60px; text-align: center; margin: 20px 0; }}
                    .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>{title}</h2>
                    </div>
                    <div class='icon'>✓</div>
                    <div class='content'>
                        <p>{greeting} {name},</p>
                        <p>{message}</p>
                        <p><strong>İletişim Numaranız:</strong> {phoneNumber}</p>
                        <p>{thanks}</p>
                    </div>
                    <div class='footer'>
                        <p>{footer}</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private string GenerateDateTimeUpdateEmailBody(string phoneNumber, string name, string preferredDate, string preferredTime, string language)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px; border-radius: 3px; }}
                    .content {{ padding: 20px 0; }}
                    .field {{ margin-bottom: 15px; }}
                    .label {{ font-weight: bold; color: #667eea; }}
                    .highlight {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; }}
                    .footer {{ text-align: center; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>📅 Randevu Tarih/Saat Tercihi Güncellendi</h2>
                    </div>
                    <div class='content'>
                        <div class='field'>
                            <span class='label'>Müşteri Adı:</span> {name}
                        </div>
                        <div class='field'>
                            <span class='label'>Telefon:</span> {phoneNumber}
                        </div>
                        
                        <div class='highlight'>
                            <div class='field'>
                                <span class='label'>📅 Tercih Edilen Tarih:</span> {preferredDate}
                            </div>
                            <div class='field'>
                                <span class='label'>🕐 Tercih Edilen Saat:</span> {preferredTime}
                            </div>
                        </div>
                        
                        <div class='field'>
                            <span class='label'>Dil:</span> {GetLanguageName(language)}
                        </div>
                        <div class='field'>
                            <span class='label'>Güncellenme Tarihi:</span> {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}
                        </div>
                    </div>
                    <div class='footer'>
                        <p>⚡ Müşteri tercih ettiği tarih ve saati belirtti.</p>
                        <p>Lütfen müşteriyle iletişime geçip randevuyu onaylayın.</p>
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

    private string GetAppointmentConfirmationSubject(string language)
    {
        return language switch
        {
            "en" => "✓ Your Appointment Request Received",
            "fr" => "✓ Votre demande de rendez-vous reçue",
            "ku" => "✓ Daxwaza berfireha we hate qebûl kirin",
            _ => "✓ Randevu Talebiniz Alındı"
        };
    }

    private (string title, string greeting, string message, string thanks, string footer) GetAppointmentConfirmationTexts(string language)
    {
        return language switch
        {
            "en" => (
                "Appointment Request Received",
                "Hello",
                "Your quick appointment request has been successfully received. Our team will contact you as soon as possible to confirm your appointment.",
                "Thank you for choosing us!",
                "X Consultation - Appointment System"
            ),
            "fr" => (
                "Demande de Rendez-vous Reçue",
                "Bonjour",
                "Votre demande de rendez-vous rapide a été reçue avec succès. Notre équipe vous contactera dès que possible pour confirmer votre rendez-vous.",
                "Merci de nous avoir choisis!",
                "X Consultation - Système de Rendez-vous"
            ),
            "ku" => (
                "Daxwaza Berfireh Hate Qebûl Kirin",
                "Bi xêr",
                "Daxwaza berfireha we ya bilez bi serkeftî hate qebûl kirin. Tîma me dê bi zû bi we re têkevin têkiliyê da ku berfireha we bipejirînin.",
                "Spas ji bo hilbijartina me!",
                "X Consultation - Pergala Berfireh"
            ),
            _ => (
                "Randevu Talebiniz Alındı",
                "Merhaba",
                "Hızlı randevu talebiniz başarıyla alınmıştır. Ekibimiz randevunuzu onaylamak için en kısa zamanda sizinle iletişime geçecektir.",
                "Bizi tercih ettiğiniz için teşekkürler!",
                "X Consultation - Randevu Sistemi"
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