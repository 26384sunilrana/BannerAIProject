# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BannerService.csproj", "."]
RUN dotnet restore "BannerService.csproj"

COPY . .
RUN dotnet build "BannerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BannerService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "BannerService.dll"]
