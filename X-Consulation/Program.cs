using X_Consulation.ContactFormApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway/Render deployment için PORT ayarı
var port = Environment.GetEnvironmentVariable("PORT") ?? "5211";
builder.WebHost.UseUrls($"http://+:{port}");

// CORS ayarları - Frontend için
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://x-consulting.vercel.app",
                "https://akayconseil.com",
                "https://www.akayconseil.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Firestore credentials
var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
if (!string.IsNullOrEmpty(firebaseJson))
{
    var tempPath = Path.Combine(Path.GetTempPath(), "firebase-creds.json");
    File.WriteAllText(tempPath, firebaseJson);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
}

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddLogging();
builder.Services.AddSingleton<FirestoreService>();

var app = builder.Build();

// CORS middleware
app.UseCors();

// Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}