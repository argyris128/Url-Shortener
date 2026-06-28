# URL Shortener Microservice

A lightweight REST microservice built with **ASP.NET Core 8** + **Entity Framework Core** + **PostgreSQL**.

## Tech Stack

- **Runtime**: .NET 8 / ASP.NET Core
- **Language**: C# 12
- **ORM**: Entity Framework Core 8 with Npgsql driver
- **Database**: PostgreSQL 16
- **Docs**: Swagger / OpenAPI via Swashbuckle
- **Containers**: Docker + docker-compose
- **Architecture**: Controller → Service → DbContext (layered, DI-wired)

## Features

- Shorten any valid URL with an auto-generated 6-char code
- Custom alias support (`/r/my-alias`)
- Click tracking with `LastClickedAt` persisted to the DB
- Full CRUD — create, list, fetch by ID, delete
- EF Core migrations applied automatically on startup
- `ExecuteUpdateAsync` for atomic bulk click-count increments (no extra round-trip)
- Request logging middleware
- Structured `ApiResponse<T>` JSON wrapper

## Run with Docker

```bash
docker-compose up --build
```

API available at `http://localhost:5000/swagger`.

Connection string is in `appsettings.json`.

## API Endpoints

`POST`    `/api/urls`                   Create a short URL     
`GET`     `/api/urls`                   List all short URLs    
`GET`     `/api/urls/{guid}`            Fetch by ID            
`DELETE`  `/api/urls/{guid}`            Delete a URL           
`GET`     `/api/urls/{shortCode}/stats`  Click analytics      
`GET`     `/r/{shortCode}`              Redirect → original    

## Architecture Notes

- `AppDbContext` — EF Core DbContext with fluent entity configuration (PK, unique index on `ShortCode`, max lengths)
- `IUrlShortenerService` — interface keeps the controller decoupled from EF Core
- `UrlShortenerService` — scoped service; uses `ExecuteUpdateAsync` for the click counter (single SQL UPDATE, no entity tracking needed)
- `db.Database.Migrate()` on startup — migrations apply automatically, zero manual steps
- Route constraints (`{id:guid}`) prevent ambiguous matches between `/api/urls/{guid}` and `/api/urls/{code}/stats`
- `ApiResponse<T>` — generic wrapper for a consistent API envelope