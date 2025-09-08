# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY src/OneTimeShare.Web/*.csproj ./src/OneTimeShare.Web/
RUN dotnet restore src/OneTimeShare.Web/OneTimeShare.Web.csproj

# Copy source code and build
COPY src/ ./src/
RUN dotnet publish src/OneTimeShare.Web/OneTimeShare.Web.csproj -c Release -o out

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create directories for data and storage
RUN mkdir -p /app/App_Data /app/storage

# Copy the published application
COPY --from=build /app/out .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OneTimeShare.Web.dll"]