# API Dockerfile — multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY VehicleIntelligence.sln ./
COPY src/VehicleIntelligence.Domain/VehicleIntelligence.Domain.csproj src/VehicleIntelligence.Domain/
COPY src/VehicleIntelligence.Application/VehicleIntelligence.Application.csproj src/VehicleIntelligence.Application/
COPY src/VehicleIntelligence.Infrastructure/VehicleIntelligence.Infrastructure.csproj src/VehicleIntelligence.Infrastructure/
COPY src/VehicleIntelligence.Grpc/VehicleIntelligence.Grpc.csproj src/VehicleIntelligence.Grpc/
COPY src/VehicleIntelligence.Api/VehicleIntelligence.Api.csproj src/VehicleIntelligence.Api/
COPY src/VehicleIntelligence.Worker/VehicleIntelligence.Worker.csproj src/VehicleIntelligence.Worker/
COPY src/VehicleIntelligence.Simulator/VehicleIntelligence.Simulator.csproj src/VehicleIntelligence.Simulator/
COPY tests/VehicleIntelligence.UnitTests/VehicleIntelligence.UnitTests.csproj tests/VehicleIntelligence.UnitTests/
COPY tests/VehicleIntelligence.IntegrationTests/VehicleIntelligence.IntegrationTests.csproj tests/VehicleIntelligence.IntegrationTests/

# Restore (cached layer)
RUN dotnet restore src/VehicleIntelligence.Api/VehicleIntelligence.Api.csproj

# Copy all source
COPY src/ src/

# Build & publish
RUN dotnet publish src/VehicleIntelligence.Api/VehicleIntelligence.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime image ──────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create log directory
RUN mkdir -p /app/logs

COPY --from=build /app/publish .

# Expose HTTP and gRPC ports
EXPOSE 8080 8081

ENTRYPOINT ["dotnet", "VehicleIntelligence.Api.dll"]
