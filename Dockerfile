# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY EduConnect.csproj .
RUN dotnet restore EduConnect.csproj

COPY . .
# SkipVideoTools: the bundled video toolchain (ffmpeg/node) is Windows-only and
# ~310 MB — pointless inside a Linux image where video generation is disabled.
RUN dotnet publish EduConnect.csproj -c Release -o /app/publish /p:SkipVideoTools=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EduConnect.dll"]
