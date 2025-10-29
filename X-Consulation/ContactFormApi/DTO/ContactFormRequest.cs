namespace X_Consulation.ContactFormApi.DTO
{
    public class ContactFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty; 
        public string Message { get; set; } = string.Empty;
        public string Language { get; set; } = "tr";
    }
}