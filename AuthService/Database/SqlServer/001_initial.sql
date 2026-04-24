IF DB_ID(N'AuthServiceDb') IS NULL
BEGIN
    CREATE DATABASE [AuthServiceDb];
END
GO

USE [AuthServiceDb];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(512) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL
    );

    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
END
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(255) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Roles_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles (Name);
END
GO

IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
        [Code] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Permissions_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_Permissions_Code ON dbo.Permissions ([Code]);
END
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_UserRoles_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleId UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_RolePermissions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id),
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermissions
    (
        UserId UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        IsGranted BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_UserPermissions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserPermissions PRIMARY KEY (UserId, PermissionId),
        CONSTRAINT FK_UserPermissions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_UserPermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id)
    );
END
GO

MERGE dbo.Roles AS target
USING
(
    VALUES
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', N'Admin', N'Full system access'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83002', N'User', N'Standard application user')
) AS source (Id, Name, Description)
ON target.Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description) VALUES (CONVERT(UNIQUEIDENTIFIER, source.Id), source.Name, source.Description);
GO

MERGE dbo.Permissions AS target
USING
(
    VALUES
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60001', N'users.view', N'View users', N'Allows listing and viewing user records'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60002', N'users.create', N'Create users', N'Allows creating new user accounts'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60003', N'users.update', N'Update users', N'Allows editing existing user accounts'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60004', N'users.delete', N'Delete users', N'Allows deleting user accounts'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60005', N'files.upload', N'Upload CSV files', N'Allows uploading transaction CSV files'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60006', N'files.view', N'View uploaded files', N'Allows viewing uploaded file metadata'),
        ('A4F1E6B8-2001-4DB7-95A4-1F6C91F60007', N'transactions.view', N'View transactions', N'Allows viewing parsed transactions')
) AS source (Id, Code, Name, Description)
ON target.Code = source.Code
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Code, Name, Description)
    VALUES (CONVERT(UNIQUEIDENTIFIER, source.Id), source.Code, source.Name, source.Description);
GO

MERGE dbo.RolePermissions AS target
USING
(
    VALUES
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60001'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60002'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60003'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60004'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60005'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60006'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83001', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60007'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83002', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60005'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83002', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60006'),
        ('9F0A6D42-A0E7-4C60-8C7D-8D3B42D83002', 'A4F1E6B8-2001-4DB7-95A4-1F6C91F60007')
) AS source (RoleId, PermissionId)
ON target.RoleId = CONVERT(UNIQUEIDENTIFIER, source.RoleId)
   AND target.PermissionId = CONVERT(UNIQUEIDENTIFIER, source.PermissionId)
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RoleId, PermissionId)
    VALUES (CONVERT(UNIQUEIDENTIFIER, source.RoleId), CONVERT(UNIQUEIDENTIFIER, source.PermissionId));
GO
