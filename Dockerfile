# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project file
COPY EduConnect.csproj .
RUN dotnet restore

# Copy source code
COPY . .
RUN dotnet build EduConnect.csproj -c Release -o /app/build

# Publish stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /var/data

# Copy built application
COPY --from=build /app/build .

# Copy static files (wwwroot)
COPY wwwroot ./wwwroot

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Run application
ENTRYPOINT ["dotnet", "EduConnect.dll"]
