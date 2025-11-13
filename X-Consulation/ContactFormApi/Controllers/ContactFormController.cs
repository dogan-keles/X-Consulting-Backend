using Microsoft.AspNetCore.Mvc;
using X_Consulation.ContactFormApi.DTO;
using X_Consulation.ContactFormApi.Models;

[HttpPost("submit")]
public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormRequest request)
{
    try
    {
        // Validasyon
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // ContactForm modeline dönüştür
        var contactForm = new ContactForm
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Topic = request.Topic,
            Message = request.Message,
            Language = request.Language,
            SubmittedAt = DateTime.UtcNow
        };

        // Firestore'a kaydet
        var documentId = await _firestoreService.AddDocumentAsync("contact_forms", contactForm);
        _logger.LogInformation($"Contact form saved with ID: {documentId}");

        // Email göndermeyi try-catch içinde yap
        // Email başarısız olsa bile form başarılı olsun
        try
        {
            // Admin'e e-posta gönder
            await _emailService.SendContactFormEmailAsync(contactForm);
            
            // Kullanıcıya onay e-postası gönder
            await _emailService.SendConfirmationEmailAsync(request.Email, request.Name, request.Language);
            
            _logger.LogInformation($"Emails sent successfully for form ID: {contactForm.Id}");
        }
        catch (Exception emailEx)
        {
            // Email hatası loglayalım ama form başarılı olsun
            _logger.LogWarning($"Email gönderilemedi ama form kaydedildi. Form ID: {contactForm.Id}, Hata: {emailEx.Message}");
        }

        _logger.LogInformation($"İletişim formu başarıyla gönderilmiştir. Form ID: {contactForm.Id}");

        return Ok(new { message = "Formunuz başarıyla gönderilmiştir.", formId = contactForm.Id });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Hata oluştu: {ex.Message}");
        return StatusCode(500, new { message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
    }
}