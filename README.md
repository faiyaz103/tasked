# Tasked

Tasked is a modular monolith application built with .NET 10. It is a simple task management system mainly focused on following proper architecture and design concepts for a backend system.

## Architecture

The project follows a **Modular Monolith** architecture.

- **Host (`src/Host/WebApi`)**: The main entry point of the application. It wires up the modules and serves the HTTP API.
- **Modules (`src/Modules`)**:
  - **Tasks Module**: Handles task-related domain logic, data access, and API endpoints.
  - **Users Module**: Handles user-related domain logic, data access, and API endpoints.
- **Shared (`src/Shared/Shared.Infra`)**: Contains common cross-cutting concerns, such as authentication logic and shared infrastructure, used across multiple modules.

## Technology Stack & Frameworks

| Technology / Framework | Description / Usage |
| :--- | :--- |
| **ASP.NET Core** | Core framework for building the RESTful Web API, handling routing, HTTP requests, and dependency injection. |
| **Entity Framework Core** | Object-Relational Mapper (ORM). |
| **PostgreSQL** | Primary relational database. |
| **FluentValidation** | Validation rules for request payloads. |
| **Authentication, Authorization & RBAC** | JWT-based **Refresh Token Rotation Policy**. |
| **Security** | Hashed passwords with `BCrypt`, Double-hashed refresh token with `SHA-256` and `BCrypt`. |
| **API Documentation** | OpenAPI endpoints are generated natively using `Microsoft.AspNetCore.OpenApi`r. |
