using Microsoft.AspNetCore.Mvc;
using X_Consulation.ContactFormApi.DTO;
using X_Consulation.ContactFormApi.Models;
using X_Consulation.ContactFormApi.Services;
using Google.Cloud.Firestore;

namespace X_Consulation.ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuickAppointmentController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<QuickAppointmentController> _logger;
    private readonly FirestoreService _firestoreService;
    private readonly FirestoreDb _firestoreDb;

    public QuickAppointmentController(
        IEmailService emailService, 
        ILogger<QuickAppointmentController> logger, 
        FirestoreService firestoreService,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _firestoreService = firestoreService;
        
        // Firestore DB instance
        string projectId = configuration["Firebase:ProjectId"];
        string credPath = configuration["Firebase:CredentialsPath"];
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
        _firestoreDb = FirestoreDb.Create(projectId);
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

            // Kullanıcıya onay bildirimi (email yok, sadece log)
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

            // Firestore'da telefon numarasına göre randevuyu bul
            var appointmentsRef = _firestoreDb.Collection("quick_appointments");
            var query = appointmentsRef.WhereEqualTo("PhoneNumber", request.PhoneNumber)
                                       .OrderByDescending("SubmittedAt")
                                       .Limit(1);
            
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                return NotFound(new { message = "Randevu bulunamadı." });
            }

            var appointmentDoc = snapshot.Documents[0];
            var appointment = appointmentDoc.ConvertTo<QuickAppointment>();
            
            // Tarih/saat bilgisini güncelle
            var updates = new Dictionary<string, object>
            {
                { "PreferredDate", request.PreferredDate ?? "" },
                { "PreferredTime", request.PreferredTime ?? "" }
            };

            await appointmentDoc.Reference.UpdateAsync(updates);

            // Admin'e email gönder (tarih/saat güncelleme bildirimi)
            await _emailService.SendDateTimeUpdateEmailAsync(
                request.PhoneNumber,
                appointment.Name,
                request.PreferredDate ?? "Belirtilmedi",
                request.PreferredTime ?? "Belirtilmedi",
                request.Language
            );

            _logger.LogInformation($"Randevu tarih/saat bilgisi güncellendi. Telefon: {request.PhoneNumber}");

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
            var appointmentsRef = _firestoreDb.Collection("quick_appointments");
            var query = appointmentsRef.OrderByDescending("SubmittedAt").Limit(50);
            var snapshot = await query.GetSnapshotAsync();

            var appointments = snapshot.Documents.Select(doc => 
            {
                var appointment = doc.ConvertTo<QuickAppointment>();
                appointment.Id = doc.Id;
                return appointment;
            }).ToList();

            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Randevular listelenirken hata oluştu: {ex.Message}");
            return StatusCode(500, new { message = "Bir hata oluştu." });
        }
    }
}