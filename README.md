# CoffeeNChill — Cloud Development PoE

## Project Overview

CoffeeNChill is a canteen management system developed incrementally using Azure Storage, Azure Functions, an ASP.NET Core Web API gateway, JWT authentication, and Docker.

## Architecture

- **Part 1:** Azure Table Storage for menu records, Blob Storage for staff documents, HTTP-triggered Azure Functions, Azurite, and standalone Docker containers.
- **Part 2:** Asynchronous order submission using `order-processing-queue`, a queue-triggered function, an `Orders` table, and Docker Compose.
- **Part 3:** ASP.NET Core API gateway, JWT authentication, role-based authorization, `Users` table, Swagger, and Linux VPS deployment.

## Technologies

- C# / .NET 8
- Azure Functions v4 — .NET isolated worker
- Azure Table Storage and Blob Storage
- Azure Queue Storage
- Azurite
- ASP.NET Core Web API
- JWT Bearer authentication
- BCrypt password hashing
- Swagger / OpenAPI
- Docker, Docker Compose, Docker Hub
- GitHub and Postman

## Part 3 API Endpoints

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| GET | `/api/menu` | Public |
| GET | `/api/menu/category/{category}` | Public |
| POST | `/api/menu` | Staff/Admin |
| PUT | `/api/menu/{category}/{id}` | Staff/Admin |
| DELETE | `/api/menu/{category}/{id}` | Staff/Admin |
| POST | `/api/orders/queue` | Public |
| PUT | `/api/orders/status` | Staff/Admin |
| POST | `/api/documents/upload` | Staff/Admin |
| GET | `/api/documents` | Staff/Admin |
| GET | `/api/documents/download/{fileName}` | Staff/Admin |
| DELETE | `/api/documents/{fileName}` | Staff/Admin |
| POST | `/api/users` | Admin only |

## Local Startup

1. Install the .NET 8 SDK, Azure Functions Core Tools v4, Docker, Git and Postman.
2. Start Azurite and the Functions application, or use the root Docker Compose setup.
3. Configure environment variables for `AzureWebJobsStorage`, `Jwt__SigningKey`, and `Functions__BaseUrl`.
4. Build the API with `dotnet restore` and `dotnet build`.
5. Run the API and open Swagger at `http://localhost:5000/swagger`.
6. Use the Postman collection to register, log in, retrieve a JWT, and test role-protected routes.

## Docker Compose

From the repository root:

```bash
docker compose pull
docker compose up -d
docker compose ps
```

The Compose file uses published Docker Hub images for Azurite, Functions, and the API gateway.

## Docker Hub Images

- Functions: `YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v3.0`
- API: `YOUR_DOCKERHUB_USERNAME/coffeenchill-api:v3.0`

## VPS Deployment

- Public gateway URL: [insert your working URL]
- Swagger URL: [insert your working Swagger URL]
- Health endpoint: [insert your working health URL]

The deployment uses Docker Compose on a Linux VPS. Secrets are provided through the server environment and are not committed to source control.

## Testing

The Postman collection is saved in `docs/`. It covers registration, login, JWT Bearer authorization, public menu access, order submission, protected menu and document operations, order status updates, and role-based access failures.

## GitHub Repository

[Insert your actual GitHub repository URL]

## Demonstration Video

[Insert your published YouTube URL]

## Group Contributions

- Student 1: [actual contributions]
- Student 2: [actual contributions]
- Student 3: [actual contributions]

## Changelog

- **Part 1:** Menu CRUD, Table Storage, Blob Storage, HTTP Functions, and Docker.
- **Part 2:** Queue-based order processing, status tracking, and Docker Compose.
- **Part 3:** JWT authentication, role-based authorization, user profiles, API gateway, Swagger, and VPS deployment.
