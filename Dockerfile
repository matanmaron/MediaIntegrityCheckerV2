FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV SCAN_PATH=/scan
VOLUME ["/scan","/config","/data","/logs"]
EXPOSE 8080
ENTRYPOINT ["dotnet","FileIntegrityService.dll"]
