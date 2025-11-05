namespace X_Consulation.ContactFormApi.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class QuickAppointment
{
    [FirestoreProperty]  
    public string Id { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Message { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string? PreferredDate { get; set; }
    
    [FirestoreProperty]
    public string? PreferredTime { get; set; }
    
    [FirestoreProperty]
    public DateTime SubmittedAt { get; set; }
    
    [FirestoreProperty]
    public string Language { get; set; } = "tr";
    
    [FirestoreProperty]
    public string Status { get; set; } = "pending"; // pending, confirmed, completed, cancelled
}