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

- JWT-based authentication (`register`, `login`)
- Wallet balance retrieval for authenticated users
- Transfer flow with **idempotency key** support
- Transaction history endpoint
- FluentValidation-based command validation
- Structured logging with Serilog (console + rolling file sinks)
- Correlation ID middleware for request tracing
- SQL Server persistence via Entity Framework Core

---

## Tech Stack

### Built With

- **.NET 10** (ASP.NET Core Web API)
- **C#**
- **Clean Architecture**
- **CQRS + MediatR**
- **FluentValidation**
- **Entity Framework Core 10**
- **SQL Server**
- **JWT Bearer Authentication**
- **Serilog**

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
- SQL Server instance (LocalDB works for development)
- Git

### Installation

```bash
git clone https://github.com/Abdul-Wajid-Ali/PayFlow.git
cd PayFlow
```

Restore dependencies:

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

### Run the API

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
│  ├─ PayFlow.API/              # Presentation layer: controllers, middleware, pipeline
│  ├─ PayFlow.Application/      # Use cases: CQRS handlers, validators, DTOs, interfaces
│  ├─ PayFlow.Domain/           # Core domain model (entities, enums, exceptions)
│  └─ PayFlow.Infrastructure/   # EF Core, repositories, external services, migrations
├─ tests/
│  ├─ PayFlow.UnitTests/
│  └─ PayFlow.IntegrationTests/
├─ PayFlow.slnx
└─ README.md
```

---

## Configuration

Configuration is primarily in:

- `src/PayFlow.API/appsettings.json`
- `src/PayFlow.API/appsettings.Development.json`

### Key Settings

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:SecretKey`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `JwtSettings:ExpiryInMinutes`
- `Serilog` sinks and log levels

### Environment Variables (recommended for production)

```bash
ConnectionStrings__DefaultConnection="<your-connection-string>"
JwtSettings__SecretKey="<your-long-random-secret>"
JwtSettings__Issuer="PayFlow.API"
JwtSettings__Audience="PayFlow.Client"
JwtSettings__ExpiryInMinutes="60"
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

- [ ] Add refresh token support and token revocation
- [ ] Add role/permission-based authorization
- [ ] Add transfer limits and configurable business rules
- [ ] Add caching for read-heavy queries
- [ ] Add comprehensive integration test scenarios with seeded data
- [ ] Add CI pipeline (build, test, lint, security scan)
- [ ] Containerize with Docker and provide compose setup
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
