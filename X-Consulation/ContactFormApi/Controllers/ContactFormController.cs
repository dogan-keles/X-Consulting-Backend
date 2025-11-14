using System;
using Microsoft.AspNetCore.Mvc;
using X_Consulation.ContactFormApi.DTO;
using X_Consulation.ContactFormApi.Models;
using X_Consulation.ContactFormApi.Services;

namespace X_Consulation.ContactFormApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactFormController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactFormController> _logger;
    private readonly FirestoreService _firestoreService;

    public ContactFormController(IEmailService emailService, ILogger<ContactFormController> logger, FirestoreService firestoreService)
    {
        _emailService = emailService;
        _logger = logger;
        _firestoreService = firestoreService;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            var documentId = await _firestoreService.AddDocumentAsync("contact_forms", contactForm);
            _logger.LogInformation($"Contact form saved with ID: {documentId}");

            try
            {
                await _emailService.SendContactFormEmailAsync(contactForm);
                await _emailService.SendConfirmationEmailAsync(request.Email, request.Name, request.Language);
                _logger.LogInformation($"Emails sent successfully for form ID: {contactForm.Id}");
            }
            catch (Exception emailEx)
            {
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
}