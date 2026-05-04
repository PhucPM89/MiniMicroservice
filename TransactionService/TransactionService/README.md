# TransactionService

TransactionService manages transaction queries, CSV import, and outbox processing for file import events.

## Requirements

- .NET 8 SDK
- SQL Server
- RabbitMQ
- A valid JWT issued by AuthService

## Configuration

Update `appsettings.json` or `appsettings.Development.json` with:

- `ConnectionStrings:TransactionDatabase`
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

The service runs on `http://localhost:5139` in the `http` profile.

## API Docs

- Swagger: `http://localhost:5139/swagger`
- Health check: `http://localhost:5139/healthz`

## Quick Smoke Test

1. Start RabbitMQ and AuthService first.
2. Start TransactionService.
3. Open Swagger and call protected transaction APIs with a JWT.
4. Verify CSV import and transaction query flows.
