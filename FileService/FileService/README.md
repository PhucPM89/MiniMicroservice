# FileService

FileService manages file upload, local file storage, outbox publishing, and file import result processing.

## Requirements

- .NET 8 SDK
- SQL Server
- RabbitMQ
- A valid JWT issued by AuthService

## Configuration

Update `appsettings.json` or `appsettings.Development.json` with:

- `ConnectionStrings:FileDatabase`
- `FileStorage:RootPath`
- `Outbox`
- `RabbitMq`
- `JwtValidation:Issuer`
- `JwtValidation:Audience`
- `JwtValidation:JwksUri`

## Run

```bash
dotnet restore
dotnet run --launch-profile http
```

The service runs on `http://localhost:5222` in the `http` profile.

## API Docs

- Swagger: `http://localhost:5222/swagger`
- Health check: `http://localhost:5222/healthz`

## Quick Smoke Test

1. Start RabbitMQ and AuthService first.
2. Start FileService.
3. Open Swagger and upload a file with a JWT.
4. Confirm files are stored under `Storage/Uploads`.
