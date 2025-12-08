# NextAdmin - Universal Admin Management Framework

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![MongoDB](https://img.shields.io/badge/MongoDB-6.0%2B-green.svg)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-7.0%2B-red.svg)](https://redis.io/)

**Enterprise-grade Universal Admin Management Framework Based on DDD (Domain-Driven Design)**

## 🎯 Project Overview

This project is a **universal framework template** built on Domain-Driven Design (DDD) principles, featuring a clear layered architecture and SOLID design principles. All specific business logic has been removed, retaining only the core infrastructure and abstractions, making it a perfect starting point for any domain application.

## 🏗️ Tech Stack

- **Backend Framework**: ASP.NET Core 9 Web API
- **Database**: MongoDB
- **Cache**: Redis
- **Message Mediator**: MediatR (Domain Events)
- **Architecture Pattern**: DDD + CQRS + Clean Architecture

## 📁 Project Structure

```
NextAdmin/
├── src/
│   ├── API/                      # Web API Layer (Presentation)
│   ├── Application/              # Application Layer (Services, DTOs)
│   ├── Core/                     # Core Layer ⭐
│   │   └── Domain/              # Domain Layer (Entities, Value Objects, Domain Events)
│   ├── Infrastructure/           # Infrastructure Layer (Data Access, External Services)
│   ├── Common/                   # Common Utilities
│   ├── Shared/                   # Shared Types
│   ├── KB0.Log/                  # Logging Service
│   └── KB0.Redis/                # Redis Service
├── .env.example                  # Environment Variables Example
├── LICENSE                       # MIT License
├── CONTRIBUTING.md               # Contribution Guidelines
└── README.md
```

## ⭐ Core Features

### 1. Domain Layer (Core/Domain)
- ✅ `BaseEntity` - Entity base class (ID, audit fields)
- ✅ `AggregateRoot` - Aggregate root (domain events, soft delete)
- ✅ `ValueObject` - Value object base class
- ✅ `DomainEventBase` - Domain event base class
- ✅ `IBaseRepository` - Generic repository interface

Detailed documentation: [Core/README.md](src/Core/README.md) | [Domain/README.md](src/Core/Domain/README.md)

### 2. Layered Architecture
- **API Layer**: RESTful API, Controllers, Middleware
- **Application Layer**: Application Services, DTOs, Mappings
- **Domain Layer**: Domain Models, Business Rules, Domain Events
- **Infrastructure Layer**: Data Access, External Service Integration

### 3. Design Patterns
- **Repository Pattern**: Data access encapsulation
- **Mediator Pattern**: MediatR for domain event handling
- **CQRS**: Command Query Responsibility Segregation
- **Dependency Injection**: Loose coupling design

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- MongoDB 6.0+
- Redis 7.0+
- Visual Studio 2022 or VS Code

### 1. Configure Environment Variables

⚠️ **Security Warning**: Never commit sensitive information from `appsettings.json` to version control!

**Recommended Approach:**

1. Copy `.env.example` to `.env` (already in `.gitignore`)
2. Configure your real connection strings and secrets in `.env`
3. Or use User Secrets:
   ```bash
   dotnet user-secrets init --project src/API
   dotnet user-secrets set "MongoDb:ConnectionString" "your_connection_string" --project src/API
   dotnet user-secrets set "Jwt:SecretKey" "your_secret_key" --project src/API
   ```

### 2. MongoDB Configuration

1. Connect to database via MongoDB Compass
2. Create admin user (**use a strong password**):
   ```javascript
   use admin
   db.createUser({
     user: "admin",
     pwd: "YOUR_STRONG_PASSWORD",  // ⚠️ Change to a strong password
     roles: ["root"]
   })
   ```

3. Enable authentication (edit `mongod.cfg`):
   ```yaml
   security:
     authorization: enabled
   ```

4. Restart MongoDB service

5. Update connection string (using environment variables or user secrets):
   ```json
   "MongoDb": {
     "ConnectionString": "mongodb://admin:YOUR_PASSWORD@localhost:27017/NextAdmin?authSource=admin",
     "DatabaseName": "NextAdmin"
   }
   ```

### 3. Run the Project

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run API project
dotnet run --project src/API/NextAdmin.API.csproj
```

Access Swagger UI: `https://localhost:5001/swagger`

**Trust Development Certificate:**
```bash
dotnet dev-certs https --trust
```

## 🐳 Docker Quick Deployment

### Start All Services with Docker Compose

```bash
# Copy environment file
cp .env.example .env

# Edit .env to set your passwords
# Start services (MongoDB + Redis)
docker-compose up -d

# Check service status
docker-compose ps

# Stop services
docker-compose down
```

### Start Database Services Only

```bash
# Start MongoDB and Redis
docker-compose up -d mongodb redis

# Run API locally
dotnet run --project src/API/NextAdmin.API.csproj
```

## 📚 Documentation

- [Project Architecture](src/Core/README.md)
- [Domain Layer Design](src/Core/Domain/README.md)
- [Dynamic Generation Mechanism](mds/TENANT_DYNAMIC_GENERATION_SUMMARY.md)
- [Contribution Guidelines](CONTRIBUTING.md)
- [Open Source Checklist](OPEN_SOURCE_CHECKLIST.md)

## 🤝 Contributing

Contributions are welcome! Please read the [Contribution Guidelines](CONTRIBUTING.md) first.

1. Fork this repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'feat: add some feature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## 🙏 Acknowledgments

- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [MongoDB](https://www.mongodb.com/)
- [MediatR](https://github.com/jbogard/MediatR)
- [AutoMapper](https://github.com/AutoMapper/AutoMapper)

## 📧 Contact

For questions or suggestions, please create an [Issue](https://github.com/YOUR_USERNAME/NextAdmin/issues)

---

⭐ If this project helps you, please give it a star!
# NextAdmin
