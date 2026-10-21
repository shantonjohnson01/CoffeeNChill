# CoffeeNChill — Part 1

## Project Overview

CoffeeNChill is a canteen management system built using C# Azure Functions, Azure Table Storage, Azure Blob Storage, and Docker.

Part 1 provides HTTP endpoints for managing menu items and staff documents. Azurite is used to emulate Azure Storage during local development.

## Technologies

- C# and .NET 8
- Azure Functions v4 — .NET isolated worker
- Azure Table Storage
- Azure Blob Storage
- Azurite
- Docker and Docker Hub
- Postman
- Git and GitHub

## Architecture

- `MenuItems`: stores menu records using Category as the PartitionKey and Item ID as the RowKey.
- `staff-docs`: Blob Storage container for staff documents.
- Azure Functions: provides HTTP endpoints for menu and document operations.
- Azurite: locally emulates Azure Storage.
- Docker: runs Azurite and the Functions application in separate containers.

## Menu Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/menu` | Create a menu item |
| GET | `/api/menu` | Get all menu items |
| GET | `/api/menu/category/{category}` | Filter menu items by category |
| PUT | `/api/menu/{category}/{id}` | Update price or availability |
| DELETE | `/api/menu/{category}/{id}` | Delete a menu item |

## Staff Document Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/documents/upload` | Upload a staff document |
| GET | `/api/documents` | List stored documents |
| GET | `/api/documents/download/{fileName}` | Download a document |

## Local Setup

1. Install .NET 8 SDK, Azure Functions Core Tools v4, Docker, Git, and Postman.
2. Start the Azurite container.
3. Set `AzureWebJobsStorage` to `UseDevelopmentStorage=true` in `local.settings.json`.
4. Open the `CoffeeNChill.Functions` directory.
5. Run `dotnet restore`.
6. Run `dotnet build`.
7. Run `func start`.
8. Test the endpoints at `http://localhost:7071` using the supplied Postman collection.

## Standalone Docker Execution

Start Azurite:

```bash
docker run -d --name coffeenchill-azurite \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

Create the Docker network and connect Azurite:

```bash
docker network create coffeenchill-net
docker network connect coffeenchill-net coffeenchill-azurite
```

Run the Functions image using the documented Azurite connection string:

```bash
docker run -d --name coffeenchill-functions \
  --network coffeenchill-net -p 7071:80 \
  -e AzureWebJobsStorage="$AZURITE_STORAGE_CONNECTION" \
  -e FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
  YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v1.0
```

Do not commit credentials or local storage settings to the repository.

## Docker Hub Images

- Functions: https://hub.docker.com/r/YOUR_DOCKERHUB_USERNAME/coffeenchill-functions
- Azurite: https://hub.docker.com/r/YOUR_DOCKERHUB_USERNAME/coffeenchill-azurite

## Postman Testing

Include the Postman collection and test:

- Menu creation, listing, category filtering, updating, and deletion.
- Staff document upload, listing, and download.
- Validation errors, duplicate menu records, and nonexistent records.

## Group Contributions

- Student 1: [name and actual contribution]
- Student 2: [name and actual contribution]
- Student 3: [name and actual contribution]

## Demonstration Video

YouTube link: [insert the actual published video URL]

The video demonstrates the running Functions application, storage integration, Postman tests, and standalone Docker containers.

## Repository

GitHub: [insert your repository URL]


## Part 2 — Queue Triggers and Docker Compose

### New Features

- Added `POST /api/orders/queue`.
- Added the `order-processing-queue` Azure Storage Queue.
- Added the `ProcessOrderQueue` background function.
- Added the `Orders` Azure Table for order tracking.
- Added status transitions: Received → Preparing → Ready → Collected.
- Added retry and poison-queue handling.
- Added `GET /api/orders/{orderId}` for demonstration and status checks.
- Added Docker Compose orchestration using the published Functions image.

### Run Part 2

1. Ensure Docker is installed and running.
2. Configure the root `.env` file using the local Azurite connection string.
3. Ensure the published Functions image is tagged `v2.0`.
4. Run `docker compose pull`.
5. Run `docker compose up -d`.
6. Test the endpoints using the Postman collection.
7. Use `docker compose logs -f functions` to view queue processing.

### Part 2 Docker Hub Image

`YOUR_DOCKERHUB_USERNAME/coffeenchill-functions:v2.0`

### Part 2 Postman Collection

`docs/CoffeeNChill-Part2.postman_collection.json`

### Changelog

- Part 1: Menu CRUD functions, Azure Table Storage, Blob Storage, standalone Docker containers.
- Part 2: Queue-based order submission, background order processing, Orders table tracking, and Docker Compose orchestration.

### Part 2 Demonstration Video

YouTube: [insert the actual published video URL]
