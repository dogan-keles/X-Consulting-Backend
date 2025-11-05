namespace X_Consulation.ContactFormApi.DTO
{
    public class QuickAppointmentRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Language { get; set; } = "tr";
    }

    public class UpdateDateTimeRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? PreferredDate { get; set; }
        public string? PreferredTime { get; set; }
        public string Language { get; set; } = "tr";
    }
}