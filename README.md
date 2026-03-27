# PayFlow

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20CQRS-0A66C2)](#architecture-overview)
[![Status](https://img.shields.io/badge/Status-Active%20Development-2EA043)](#roadmap)
[![License](https://img.shields.io/badge/License-[PLACEHOLDER]-lightgrey)](#license)

> A cleanly layered digital wallet and transfer API built with **Clean Architecture + CQRS** on **.NET 10**.

PayFlow is a backend-focused project designed to demonstrate production-style API design for authentication, wallet balances, and peer-to-peer transfers. It emphasizes maintainability, testability, and clear separation of concerns.

---

## Table of Contents

- [Why PayFlow?](#why-payflow)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture Overview](#architecture-overview)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Run the API](#run-the-api)
- [Usage](#usage)
  - [Authentication](#authentication)
  - [Wallet](#wallet)
  - [Transfers](#transfers)
- [API Documentation](#api-documentation)
- [Folder Structure](#folder-structure)
- [Configuration](#configuration)
- [Testing](#testing)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

---

## Why PayFlow?

This project uses **Clean Architecture** and **CQRS** intentionally:

- **Clean boundaries** keep business logic independent of frameworks and infrastructure.
- **CQRS with MediatR** separates read and write concerns, making use cases explicit and easier to evolve.
- **Pipeline behaviors** centralize cross-cutting concerns (validation/logging) instead of duplicating them in handlers.
- **Global exception handling + problem details** improves API consistency for consumers.

> **Purpose:** `Learning`

---

## Features

- JWT-based authentication (`register`, `login`) with refresh token support
- Wallet balance retrieval for authenticated users
- Transfer flow with **idempotency key** support and conflict handling
- Transaction history endpoint with pagination
- FluentValidation-based command validation
- Structured logging with Serilog (console + rolling file sinks)
- Correlation ID middleware for request tracing
- SQL Server persistence via Entity Framework Core
- Redis caching for read-heavy queries (wallet balance)
- RabbitMQ message broker integration
- Health checks for SQL Server and Redis
- API versioning (v1/v2)
- Transfer rate limiting (configurable)
- Docker containerization with multi-stage builds
- Docker Compose orchestration (API, SQL Server, Redis, RabbitMQ)

---

## Tech Stack

### Built With

- **.NET 10** (ASP.NET Core Web API)
- **C#**
- **Clean Architecture**
- **CQRS + MediatR 12.5**
- **FluentValidation 12.1**
- **Entity Framework Core 10**
- **SQL Server 2022**
- **Redis 7.2** (StackExchange.Redis 2.12.8) — caching
- **RabbitMQ 4.1** — message broker
- **JWT Bearer Authentication**
- **Serilog 10**
- **API Versioning** (Asp.Versioning.Mvc 8.1.1)
- **Health Checks** (AspNetCore.HealthChecks for SQL Server & Redis)
- **Docker & Docker Compose** — containerization

---

## Architecture Overview

PayFlow follows a layered architecture:

- **PayFlow.Domain**
  - Core entities, enums, and domain exceptions
  - No dependencies on outer layers
- **PayFlow.Application**
  - Use cases (`Commands`/`Queries`) and handlers
  - DTOs, validators, abstractions (interfaces)
  - MediatR pipeline behaviors for validation/logging
- **PayFlow.Infrastructure**
  - EF Core `DbContext`, repositories, migrations
  - Implementations for JWT, password hashing, current user context, date/time
- **PayFlow.API**
  - Controllers, middleware, exception handlers, DI wiring
  - AuthN/AuthZ setup and HTTP pipeline

### Request Flow (High Level)

1. Request hits controller (`API`).
2. Controller dispatches command/query through MediatR (`Application`).
3. Behaviors run (validation/logging).
4. Handler executes business logic using interfaces.
5. Repositories/services in `Infrastructure` persist/fetch data.
6. Response DTO is returned to caller.

This structure reduces coupling and enables easier unit/integration testing per layer.

---

## Getting Started

### Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/) (recommended) **or** a local SQL Server, Redis, and RabbitMQ instance
- Git

### Installation

```bash
git clone https://github.com/Abdul-Wajid-Ali/PayFlow.git
cd PayFlow
```

### Run with Docker (Recommended)

The fastest way to get started — Docker Compose brings up the API and all its dependencies (SQL Server, Redis, RabbitMQ) in one command:

```bash
docker-compose up -d
```

| Service        | Port(s)              | Notes                          |
|----------------|----------------------|--------------------------------|
| PayFlow API    | `5000` (HTTP), `5001` (HTTPS) | ASP.NET Core application |
| SQL Server     | `1433`               | SA password: `PayFlow@123!`    |
| Redis          | `6379`               | AOF persistence enabled        |
| RabbitMQ       | `5672` (AMQP), `15672` (Management UI) | Credentials: `payflow` / `payflow123` |

All services include health checks and persistent volumes. The API waits for SQL Server and Redis to be healthy before starting.

To stop all services:

```bash
docker-compose down
```

To stop and remove volumes (reset data):

```bash
docker-compose down -v
```

### Run with .NET CLI (Alternative)

If you prefer running without Docker, ensure you have SQL Server, Redis, and RabbitMQ running locally, then:

```bash
dotnet restore
```

Apply database migrations (from repo root):

```bash
dotnet ef database update --project src/PayFlow.Infrastructure --startup-project src/PayFlow.API
```

> If `dotnet ef` is not installed:
>
> ```bash
> dotnet tool install --global dotnet-ef
> ```

Run the API:

```bash
dotnet run --project src/PayFlow.API
```

Default development URL is defined in launch settings.

---

## Usage

Base URL (development):

```text
https://localhost:5001
```

> Confirm the exact port from `src/PayFlow.API/Properties/launchSettings.json` in your environment.

### Authentication

#### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "StrongP@ssw0rd!"
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "StrongP@ssw0rd!"
}
```

Use the returned JWT in the `Authorization` header:

```http
Authorization: Bearer <token>
```

### Wallet

#### Get balance

```http
GET /api/wallet/balance
Authorization: Bearer <token>
```

#### Get transactions

```http
GET /api/wallet/transactions
Authorization: Bearer <token>
```

### Transfers

#### Create transfer

```http
POST /api/transfer
Authorization: Bearer <token>
Idempotency-Key: <unique-key>
Content-Type: application/json

{
  "receiverUserId": "<guid>",
  "amount": 25.00,
  "currency": "USD"
}
```

---

## API Documentation

OpenAPI is enabled in development mode.

After running locally, use:

- OpenAPI document: `GET /openapi/v1.json` `[PLACEHOLDER: verify actual endpoint name/version]`
- Optional: import endpoint into Postman/Insomnia

You can also use `src/PayFlow.API/PayFlow.API.http` for quick local requests.

---

## Folder Structure

```text
PayFlow/
├─ src/
│  ├─ PayFlow.API/
│  │  ├─ Controllers/V1/, V2/   # Versioned API controllers
│  │  ├─ Middlewares/            # Correlation ID, etc.
│  │  ├─ ExceptionHandlers/     # Global exception handling
│  │  ├─ Extensions/            # Service registration extensions
│  │  ├─ RateLimiting/          # Rate limiting configuration
│  │  ├─ Dockerfile             # Multi-stage Docker build
│  │  └─ Program.cs             # Entry point & DI wiring
│  ├─ PayFlow.Application/      # Use cases: CQRS handlers, validators, DTOs, interfaces
│  ├─ PayFlow.Domain/           # Core domain model (entities, enums, exceptions)
│  └─ PayFlow.Infrastructure/   # EF Core, repositories, external services, migrations
├─ tests/
│  ├─ PayFlow.UnitTests/
│  └─ PayFlow.IntegrationTests/
├─ docker-compose.yml            # Full stack: API + SQL Server + Redis + RabbitMQ
├─ docker-compose.override.yml   # Development overrides (secrets, certs)
├─ .dockerignore
├─ PayFlow.slnx
└─ README.md
```

---

## Configuration

Configuration is primarily in:

- `src/PayFlow.API/appsettings.json`
- `src/PayFlow.API/appsettings.Development.json`

### Key Settings

- `ConnectionStrings:DefaultConnection` — SQL Server connection string
- `JwtSettings:SecretKey` / `Issuer` / `Audience` / `ExpiryInMinutes`
- `JwtSettings:RefreshTokenExpiryInDays` — refresh token lifetime (default: 14)
- `Redis:ConnectionString` — Redis connection (default: `localhost:6379`)
- `RabbitMQ:HostName` / `Port` / `UserName` / `Password` — message broker
- `RabbitMQ:Exchange` / `RetryCount` — exchange name and retry policy
- `RateLimiting:Transfers:PermitLimit` — max transfer requests per window (default: 5)
- `RateLimiting:Transfers:WindowInSeconds` — rate limit window (default: 60)
- `Serilog` sinks and log levels

### Environment Variables (recommended for production)

```bash
ConnectionStrings__DefaultConnection="<your-connection-string>"
JwtSettings__SecretKey="<your-long-random-secret>"
JwtSettings__Issuer="PayFlow.API"
JwtSettings__Audience="PayFlow.Client"
JwtSettings__ExpiryInMinutes="60"
JwtSettings__RefreshTokenExpiryInDays="14"
Redis__ConnectionString="<redis-host>:6379"
RabbitMQ__HostName="<rabbitmq-host>"
RabbitMQ__Port="5672"
RabbitMQ__UserName="payflow"
RabbitMQ__Password="<password>"
ASPNETCORE_ENVIRONMENT="Production"
```

> Never commit production secrets. Use secret managers / CI environment variables.

---

## Testing

Run all tests:

```bash
dotnet test
```

Run a specific test project:

```bash
dotnet test tests/PayFlow.UnitTests
dotnet test tests/PayFlow.IntegrationTests
```

> `[PLACEHOLDER]` Add test coverage badge/reporting (e.g., Coverlet + ReportGenerator) when configured.

---

## Roadmap

- [x] Add refresh token support and token revocation
- [x] Add caching for read-heavy queries (Redis)
- [x] Containerize with Docker and provide compose setup
- [ ] Add role/permission-based authorization
- [ ] Add transfer limits and configurable business rules
- [ ] Add comprehensive integration test scenarios with seeded data
- [ ] Add CI pipeline (build, test, lint, security scan)
- [ ] Add observability (metrics/tracing dashboards)

---

## Contributing

Contributions are welcome.

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m "feat: add your feature"`
4. Push to your branch: `git push origin feature/your-feature`
5. Open a Pull Request

Please keep changes aligned with Clean Architecture boundaries and include tests where relevant.

---

## License

`[PLACEHOLDER: Add your license, e.g., MIT]`

If you choose MIT, add a `LICENSE` file and replace this section accordingly.

---

## Contact

**Author:** `Wajid ALi`  
**GitHub:** `https://github.com/Abdul-Wajid-Ali`  
**Email:** `developer.wajidali@gmail.com`

---

If this repository is part of your portfolio, consider pinning it and adding a short demo/video link here.