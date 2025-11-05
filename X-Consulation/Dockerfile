# .NET 8 runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Railway 8080 portunu kullanır
EXPOSE 8080

# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# HATA DÜZELTMESİ: Proje yolu, alt klasöre göre ayarlandı.
# Log'a göre klasör adı "X-Consulation" (tek 'l' ile) olmalıdır.
RUN dotnet restore "./X-Consulation/X-Consultation.csproj"

# HATA DÜZELTMESİ: Proje yolu, alt klasöre göre ayarlandı.
RUN dotnet publish "./X-Consulation/X-Consultation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final aşaması
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# HATA DÜZELTMESİ: Çalıştırılacak DLL adı güncellendi.
# DLL dosyasının, projenin derlenmiş adı (X-Consultation) ile aynı olmasi gerekir.
ENTRYPOINT ["dotnet", "X-Consultation.dll"]