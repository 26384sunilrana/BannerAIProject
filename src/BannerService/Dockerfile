# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BannerService.csproj", "."]
RUN dotnet restore "BannerService.csproj"

COPY . .
RUN dotnet publish "BannerService.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Data Protection keys and logs live on mounted volumes
RUN mkdir -p /keys /app/logs && chown -R app:app /keys /app/logs
USER app

ENV ASPNETCORE_URLS=http://+:5000 \
    Security__KeyDirectory=/keys
EXPOSE 5000

ENTRYPOINT ["dotnet", "BannerService.dll"]
