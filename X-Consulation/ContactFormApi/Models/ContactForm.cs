namespace X_Consulation.ContactFormApi.Models;

using Google.Cloud.Firestore;
[FirestoreData]
public class ContactForm
{
    [FirestoreProperty]  
    public string Id { get; set; } = string.Empty;
    [FirestoreProperty]
    public string Name { get; set; }= string.Empty;
    [FirestoreProperty]
    public string PhoneNumber { get; set; } = string.Empty;
    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;
    [FirestoreProperty]
    public string Topic { get; set; } = string.Empty;
    [FirestoreProperty]
    public string Message { get; set; } = string.Empty;
    [FirestoreProperty]
    public DateTime SubmittedAt { get; set; }
    [FirestoreProperty]
    public string Language { get; set; } = "tr";
    
}