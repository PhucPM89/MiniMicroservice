IF DB_ID(N'FileServiceDb') IS NULL
BEGIN
    CREATE DATABASE [FileServiceDb];
END
GO

USE [FileServiceDb];
GO

IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessages
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
        [Exchange] NVARCHAR(150) NOT NULL,
        RoutingKey NVARCHAR(150) NOT NULL,
        MessageKey NVARCHAR(120) NOT NULL,
        MessageType NVARCHAR(250) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        OccurredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_OutboxMessages_OccurredAtUtc DEFAULT (SYSUTCDATETIME()),
        ProcessedAtUtc DATETIME2 NULL,
        LastAttemptAtUtc DATETIME2 NULL,
        LockedUntilUtc DATETIME2 NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_OutboxMessages_AttemptCount DEFAULT (0),
        LockId NVARCHAR(100) NULL,
        LastError NVARCHAR(2000) NULL
    );

    CREATE INDEX IX_OutboxMessages_ProcessState ON dbo.OutboxMessages (ProcessedAtUtc, LockedUntilUtc, OccurredAtUtc);
    CREATE INDEX IX_OutboxMessages_Routing ON dbo.OutboxMessages ([Exchange], RoutingKey, MessageKey);
END
GO

IF OBJECT_ID(N'dbo.InboxMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboxMessages
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InboxMessages PRIMARY KEY,
        MessageId NVARCHAR(120) NOT NULL,
        Consumer NVARCHAR(120) NOT NULL,
        [Exchange] NVARCHAR(150) NOT NULL,
        RoutingKey NVARCHAR(150) NOT NULL,
        MessageType NVARCHAR(250) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        ReceivedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_InboxMessages_ReceivedAtUtc DEFAULT (SYSUTCDATETIME()),
        LastAttemptAtUtc DATETIME2 NULL,
        ProcessedAtUtc DATETIME2 NULL,
        LockedUntilUtc DATETIME2 NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_InboxMessages_AttemptCount DEFAULT (0),
        LockId NVARCHAR(100) NULL,
        LastError NVARCHAR(2000) NULL
    );

    CREATE UNIQUE INDEX UX_InboxMessages_MessageId_Consumer ON dbo.InboxMessages (MessageId, Consumer);
    CREATE INDEX IX_InboxMessages_ProcessState ON dbo.InboxMessages (ProcessedAtUtc, LockedUntilUtc, ReceivedAtUtc);
END
GO

IF OBJECT_ID(N'dbo.Files', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Files
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Files PRIMARY KEY,
        OriginalFileName NVARCHAR(255) NOT NULL,
        StoredFileName NVARCHAR(255) NOT NULL,
        StoragePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,
        FileExtension NVARCHAR(20) NOT NULL,
        SizeInBytes BIGINT NOT NULL,
        UploadedByUserId UNIQUEIDENTIFIER NOT NULL,
        CorrelationId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Files_Status DEFAULT (N'Uploaded'),
        ErrorMessage NVARCHAR(1000) NULL,
        UploadedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Files_UploadedAtUtc DEFAULT (SYSUTCDATETIME()),
        QueuedAtUtc DATETIME2 NULL,
        ProcessedAtUtc DATETIME2 NULL,
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT CK_Files_Status CHECK (Status IN (N'Uploaded', N'Queued', N'Processing', N'Completed', N'Failed'))
    );

    CREATE UNIQUE INDEX UX_Files_StoredFileName ON dbo.Files (StoredFileName);
    CREATE UNIQUE INDEX UX_Files_CorrelationId ON dbo.Files (CorrelationId) WHERE CorrelationId IS NOT NULL;
    CREATE INDEX IX_Files_UploadedByUserId ON dbo.Files (UploadedByUserId);
    CREATE INDEX IX_Files_Status ON dbo.Files (Status);
END
GO
