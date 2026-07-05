# Real-Time Vehicle Intelligence Platform

A high-performance, production-ready IoT telemetry processing and rule evaluation engine built on .NET 8, gRPC streaming, event-driven architecture, PostgreSQL, Redis, and RabbitMQ.

## Architecture & Design Patterns

The platform is designed using modern architectural paradigms:
- **Clean Architecture**: Decoupling core business rules (Domain/Application) from external concerns (Infrastructure/API).
- **gRPC Client Streaming**: Real-time high-throughput telemetry ingestion from connected vehicles.
- **Event-Driven Processing**: MassTransit + RabbitMQ are used to dispatch and consume telemetry processing events out-of-box.
- **Rules Engine**: Extensible pipeline evaluating speeding, thermal anomalies, battery depletion, and fuel rates.
- **Risk Scoring**: Aggregate scoring algorithm calculating real-time safety indices for vehicles.
- **Redis Cache**: Caches real-time status and pre-aggregated dashboards for instant reads.

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose

### 1. Infrastructure Setup
Spin up PostgreSQL, Redis, and RabbitMQ containers:
```bash
docker compose up -d
```

### 2. Database Migrations
Migrations are auto-applied at startup, but you can manually apply migrations via EF Core CLI:
```bash
dotnet ef database update --project src/VehicleIntelligence.Infrastructure --startup-project src/VehicleIntelligence.Api
```

### 3. Running the Services
Start the REST/gRPC API and the Event-Driven Worker background service:
```bash
# In Terminal 1 (API)
dotnet run --project src/VehicleIntelligence.Api

# In Terminal 2 (Worker)
dotnet run --project src/VehicleIntelligence.Worker
```

### 4. Telemetry Simulation
Replay anomalous vehicle datasets into the streaming API:
```bash
dotnet run --project src/VehicleIntelligence.Simulator
```

---

## Security & Secrets Management

To prevent exposing database or broker passwords in git repository commits, the platform implements a multi-tiered configuration strategy:

### 1. Local Development Defaults
- The `appsettings.json` files contain default values targeting local development containers. These are safe to commit as they only represent local sandbox credentials.
- To use custom local passwords without committing them, use **.NET User Secrets**:
  ```bash
  # Override API database password locally
  dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Database=vehicleintelligence;Username=vehicleadmin;Password=YOUR_SECURE_PASSWORD" --project src/VehicleIntelligence.Api
  ```

### 2. Docker Compose Environment Variables (`.env`)
- The `docker-compose.yml` uses placeholders (`${DB_PASSWORD:-vehiclepass123}`) to inject passwords dynamically.
- Copy `.env.template` to `.env` and fill in your custom credentials before deployment:
  ```bash
  cp .env.template .env
  ```
- `.env` is automatically ignored by `.gitignore`.

### 3. Production Configurations
- In production (e.g. Kubernetes, AWS ECS, Azure App Service), override connection strings by injecting **Environment Variables**:
  - `ConnectionStrings__PostgreSQL`: PostgreSQL connection string.
  - `RabbitMQ__Password`: RabbitMQ user password.
  - `Redis__ConnectionString`: Redis server host.
