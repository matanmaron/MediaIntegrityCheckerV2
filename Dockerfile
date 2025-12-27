# Use .NET 8 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published files
COPY --from=build /publish .

# Expose port
EXPOSE 5000

# Create folders inside container (will be mounted from host)
RUN mkdir -p /scan /data /logs

# Environment variable for scan path (CasaOS Docker UI can override)
ENV ScanDirectory=/scan

ENTRYPOINT ["dotnet", "MediaIntegrityCheckerV2.dll"]
