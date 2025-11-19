using Microsoft.AspNetCore.Mvc;
using X_Consulation.ContactFormApi.DTO;
using X_Consulation.ContactFormApi.Models;
using X_Consulation.ContactFormApi.Services;

namespace X_Consulation.ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuickAppointmentController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<QuickAppointmentController> _logger;
    private readonly FirestoreService _firestoreService;

    public QuickAppointmentController(
        IEmailService emailService, 
        ILogger<QuickAppointmentController> logger, 
        FirestoreService firestoreService)
    {
        _emailService = emailService;
        _logger = logger;
        _firestoreService = firestoreService;
    }

    [HttpPost("quick-submit")]
    public async Task<IActionResult> SubmitQuickAppointment([FromBody] QuickAppointmentRequest request)
    {
        try
        {
            // Validasyon
            if (string.IsNullOrEmpty(request.PhoneNumber) || 
                string.IsNullOrEmpty(request.Name) || 
                string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { message = "Telefon, isim ve mesaj alanları zorunludur." });
            }

            // QuickAppointment modeline dönüştür
            var appointment = new QuickAppointment
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = request.PhoneNumber,
                Name = request.Name,
                Message = request.Message,
                Language = request.Language,
                SubmittedAt = DateTime.UtcNow,
                Status = "pending"
            };

            // Firestore'a kaydet
            var documentId = await _firestoreService.AddDocumentAsync("quick_appointments", appointment);
            _logger.LogInformation($"Quick appointment saved with ID: {documentId}");

            // Admin'e e-posta gönder
            await _emailService.SendQuickAppointmentEmailAsync(appointment);

            // Kullanıcıya onay bildirimi
            await _emailService.SendAppointmentConfirmationEmailAsync(
                request.PhoneNumber, 
                request.Name, 
                request.Language);

            _logger.LogInformation($"Hızlı randevu başarıyla kaydedildi. ID: {appointment.Id}");

            return Ok(new { 
                success = true,
                message = "Randevu talebiniz başarıyla alınmıştır.", 
                appointmentId = appointment.Id,
                phoneNumber = request.PhoneNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Hata oluştu: {ex.Message}");
            return StatusCode(500, new { 
                success = false,
                message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." 
            });
        }
    }

    [HttpPost("update-datetime")]
    public async Task<IActionResult> UpdateDateTime([FromBody] UpdateDateTimeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                return BadRequest(new { message = "Telefon numarası gerekli." });
            }

            // Not: Bu endpoint için FirestoreService'e query metodu eklenmelidir
            // Şimdilik basit implementasyon
            _logger.LogInformation($"DateTime update request for: {request.PhoneNumber}");

            // Admin'e email gönder (tarih/saat güncelleme bildirimi)
            await _emailService.SendDateTimeUpdateEmailAsync(
                request.PhoneNumber,
                "Kullanıcı", // Name bulunamadı, placeholder
                request.PreferredDate ?? "Belirtilmedi",
                request.PreferredTime ?? "Belirtilmedi",
                request.Language
            );

            return Ok(new { 
                success = true,
                message = "Tercih ettiğiniz tarih/saat kaydedildi." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Tarih/saat güncellenirken hata oluştu: {ex.Message}");
            return StatusCode(500, new { 
                success = false,
                message = "Bir hata oluştu." 
            });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListAppointments()
    {
        try
        {
            // Not: Bu endpoint için FirestoreService'e list metodu eklenmelidir
            // Şimdilik placeholder response
            return Ok(new List<QuickAppointment>());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Randevular listelenirken hata oluştu: {ex.Message}");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }
}