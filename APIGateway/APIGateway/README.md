# APIGateway

APIGateway is the reverse proxy entry point for AuthService, FileService, and TransactionService.

## Requirements

- .NET 8 SDK
- AuthService, FileService, and TransactionService running locally

## Configuration

Update `appsettings.json` or `appsettings.Development.json` with:

- `GatewayJwt:Issuer`
- `GatewayJwt:Audience`
- `GatewayJwt:JwksUri`
- `ReverseProxy:Routes`
- `ReverseProxy:Clusters`

## Run

```bash
dotnet restore
dotnet run --launch-profile http
```

The gateway runs on `http://localhost:5201` in the `http` profile.

## API Docs

- Swagger: `http://localhost:5201/swagger`
- Health check: `http://localhost:5201/healthz`

## Quick Smoke Test

1. Start AuthService, FileService, and TransactionService first.
2. Start APIGateway.
3. Call auth through `/api/auth/*`.
4. Call protected routes through `/api/users/*`, `/api/files/*`, and `/api/transactions/*`.
