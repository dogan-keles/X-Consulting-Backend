# .NET 8 runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./X-Consultation.csproj"
RUN dotnet publish "./X-Consultation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final aşaması
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "X-Consultation.dll"]