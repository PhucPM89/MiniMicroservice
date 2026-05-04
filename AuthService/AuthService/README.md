# AuthService

AuthService handles authentication, user management, roles, permissions, and JWT token issuance.

## Requirements

- .NET 8 SDK
- SQL Server
- Access to the RSA key files in `Keys/`

## Configuration

Update `appsettings.json` or `appsettings.Development.json` with:

- `ConnectionStrings:AuthDatabase`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:PrivateKeyPath`
- `Jwt:PublicKeyPath`

## Run

```bash
dotnet restore
dotnet run --launch-profile http
```

The service runs on `http://localhost:5172` in the `http` profile.

## API Docs

- Swagger: `http://localhost:5172/swagger`
- JWKS: `http://localhost:5172/.well-known/jwks.json`

## Quick Smoke Test

1. Start the service.
2. Open Swagger.
3. Call the auth endpoints to obtain a JWT.
4. Use the JWT for protected user/permission APIs.
