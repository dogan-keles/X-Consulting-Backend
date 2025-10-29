namespace X_Consulation.ContactFormApi.Services;
using Google.Cloud.Firestore;
public class FirestoreService
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreService(IConfiguration configuration)
    {
        string projectId = configuration["Firebase:ProjectId"];
        string credPath = configuration["Firebase:CredentialsPath"];
        
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
        _firestoreDb = FirestoreDb.Create(projectId);
    }
    public async Task<string> AddDocumentAsync<T>(string collectionName, T data)
    {
        var docRef = await _firestoreDb.Collection(collectionName).AddAsync(data);
        return docRef.Id;
    }
    
}