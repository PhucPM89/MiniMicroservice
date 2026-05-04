# MiniMicroservice

Mono-repo cho hệ thống microservice gồm AuthService, APIGateway, FileService, TransactionService, FileManagement frontend, và Shared library.

## Repo Layout

- `AuthService`: xác thực, user, role, permission, JWT, JWKS.
- `APIGateway`: reverse proxy entry point cho các API backend.
- `FileService`: upload file, lưu storage local, outbox, consume kết quả import.
- `TransactionService`: xử lý transaction, import CSV, outbox, consume file import event.
- `FileManagement`: frontend Vite/React.
- `Shared`: các base class, constants, messaging, result, pagination dùng chung.

## Start Order

Chạy theo thứ tự này để tránh lỗi dependency:

1. RabbitMQ.
2. SQL Server.
3. AuthService.
4. FileService.
5. TransactionService.
6. APIGateway.
7. FileManagement frontend.

## Local URLs

- AuthService: `http://localhost:5172`
- FileService: `http://localhost:5222`
- TransactionService: `http://localhost:5139`
- APIGateway: `http://localhost:5201`
- Frontend: `http://localhost:5173`

## End-to-End Flow

1. Người dùng đăng nhập qua AuthService hoặc qua `/api/auth/*` trên APIGateway.
2. AuthService phát hành JWT và expose JWKS tại `/.well-known/jwks.json`.
3. Frontend dùng JWT để gọi API qua APIGateway.
4. APIGateway kiểm tra JWT rồi forward request sang service đích.
5. FileService xử lý upload file và ghi dữ liệu cần thiết vào DB/queue.
6. TransactionService nhận dữ liệu import và xử lý transaction/import flow.
7. Hai service backend dùng RabbitMQ cho outbox/consumer để đồng bộ event giữa các service.

## Quick Run

```bash
dotnet restore
dotnet run --project AuthService/AuthService/AuthService.csproj --launch-profile http
dotnet run --project FileService/FileService/FileService.csproj --launch-profile http
dotnet run --project TransactionService/TransactionService/TransactionService.csproj --launch-profile http
dotnet run --project APIGateway/APIGateway/APIGateway.csproj --launch-profile http
```

Nếu chạy frontend:

```bash
cd FileManagement/FileManagement
npm install
npm run dev
```

## Notes

- Swagger đã được bật ở các service backend trong môi trường Development.
- Khi test local, nhớ cập nhật connection string, RabbitMQ credentials, và JWT/JWKS URL trong `appsettings.json` hoặc `appsettings.Development.json`.
- Local file storage hiện đang dùng `Storage/Uploads` trong `FileService`.