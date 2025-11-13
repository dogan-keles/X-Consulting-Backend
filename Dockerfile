# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["X-Consulation/X-Consulation.csproj", "X-Consulation/"]
RUN dotnet restore "X-Consulation/X-Consulation.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/X-Consulation"
RUN dotnet build "X-Consulation.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "X-Consulation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables for Render/Railway
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published files
COPY --from=publish /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "X-Consulation.dll"]

