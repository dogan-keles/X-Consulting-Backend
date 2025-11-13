namespace X_Consulation.ContactFormApi.Services;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;

public class FirestoreService
{
    private readonly FirestoreDb _firestoreDb;
    
    public FirestoreService(IConfiguration configuration)
    {
        string projectId = configuration["Firebase:ProjectId"] ?? "x-consulation";
        
        // Render'da environment variable'dan oku
        string credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
        
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            // Production (Render) - JSON string'den credential oluştur
            GoogleCredential credential = GoogleCredential.FromJson(credentialsJson);
            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            }.Build();
        }
        else
        {
            // Local development - dosya yolundan oku
            string credPath = configuration["Firebase:CredentialsPath"];
            if (!string.IsNullOrEmpty(credPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
            }
            _firestoreDb = FirestoreDb.Create(projectId);
        }
    }
    
    public async Task<string> AddDocumentAsync<T>(string collectionName, T data)
    {
        var docRef = await _firestoreDb.Collection(collectionName).AddAsync(data);
        return docRef.Id;
    }
}