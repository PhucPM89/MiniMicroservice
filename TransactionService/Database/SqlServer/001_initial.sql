IF DB_ID(N'TransactionServiceDb') IS NULL
BEGIN
    CREATE DATABASE [TransactionServiceDb];
END
GO

USE [TransactionServiceDb];
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

IF OBJECT_ID(N'dbo.ImportBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportBatches
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ImportBatches PRIMARY KEY,
        FileId UNIQUEIDENTIFIER NOT NULL,
        UploadedByUserId UNIQUEIDENTIFIER NULL,
        FileName NVARCHAR(255) NOT NULL,
        CorrelationId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_ImportBatches_Status DEFAULT (N'Pending'),
        TotalRecords INT NOT NULL CONSTRAINT DF_ImportBatches_TotalRecords DEFAULT (0),
        SuccessfulRecords INT NOT NULL CONSTRAINT DF_ImportBatches_SuccessfulRecords DEFAULT (0),
        FailedRecords INT NOT NULL CONSTRAINT DF_ImportBatches_FailedRecords DEFAULT (0),
        ErrorMessage NVARCHAR(1000) NULL,
        StartedAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ImportBatches_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT CK_ImportBatches_Status CHECK (Status IN (N'Pending', N'Processing', N'Completed', N'Failed'))
    );

    CREATE UNIQUE INDEX UX_ImportBatches_FileId ON dbo.ImportBatches (FileId);
    CREATE INDEX IX_ImportBatches_UploadedByUserId ON dbo.ImportBatches (UploadedByUserId);
    CREATE UNIQUE INDEX UX_ImportBatches_CorrelationId ON dbo.ImportBatches (CorrelationId) WHERE CorrelationId IS NOT NULL;
END
GO

IF COL_LENGTH(N'dbo.ImportBatches', N'UploadedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ImportBatches
    ADD UploadedByUserId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportBatches_UploadedByUserId'
      AND object_id = OBJECT_ID(N'dbo.ImportBatches')
)
BEGIN
    CREATE INDEX IX_ImportBatches_UploadedByUserId ON dbo.ImportBatches (UploadedByUserId);
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
        ImportBatchId UNIQUEIDENTIFIER NOT NULL,
        TransactionId NVARCHAR(100) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        RawLineNumber INT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Transactions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_Transactions_ImportBatches FOREIGN KEY (ImportBatchId) REFERENCES dbo.ImportBatches (Id)
    );

    CREATE INDEX IX_Transactions_ImportBatchId ON dbo.Transactions (ImportBatchId);
    CREATE UNIQUE INDEX UX_Transactions_ImportBatchId_TransactionId ON dbo.Transactions (ImportBatchId, TransactionId);
    CREATE INDEX IX_Transactions_TransactionId ON dbo.Transactions (TransactionId);
    CREATE INDEX IX_Transactions_Type ON dbo.Transactions (Type);
END
GO

IF OBJECT_ID(N'dbo.TransactionErrors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionErrors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TransactionErrors PRIMARY KEY,
        ImportBatchId UNIQUEIDENTIFIER NOT NULL,
        LineNumber INT NOT NULL,
        RawRecord NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(1000) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TransactionErrors_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TransactionErrors_ImportBatches FOREIGN KEY (ImportBatchId) REFERENCES dbo.ImportBatches (Id)
    );

    CREATE INDEX IX_TransactionErrors_ImportBatchId ON dbo.TransactionErrors (ImportBatchId);
    CREATE INDEX IX_TransactionErrors_LineNumber ON dbo.TransactionErrors (LineNumber);
END
GO
