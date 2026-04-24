using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using TransactionService.Entities;

namespace TransactionService.Infrastructure.Persistence;

public sealed class TransactionServiceDbContext(DbContextOptions<TransactionServiceDbContext> options) : DbContext(options)
{
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    public DbSet<TransactionErrorRecord> TransactionErrors => Set<TransactionErrorRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.ToTable("ImportBatches");
            entity.HasKey(batch => batch.Id);

            entity.Property(batch => batch.UploadedByUserId);

            entity.Property(batch => batch.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(batch => batch.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(batch => batch.ErrorMessage)
                .HasMaxLength(1000);

            entity.HasIndex(batch => batch.FileId)
                .IsUnique();

            entity.HasIndex(batch => batch.UploadedByUserId);

            entity.HasIndex(batch => batch.CorrelationId)
                .IsUnique()
                .HasFilter("[CorrelationId] IS NOT NULL");
        });

        modelBuilder.Entity<TransactionRecord>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(transaction => transaction.Id);

            entity.Property(transaction => transaction.TransactionId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(transaction => transaction.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(transaction => transaction.Type)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(transaction => transaction.Description)
                .HasMaxLength(500);

            entity.HasOne(transaction => transaction.ImportBatch)
                .WithMany(batch => batch.Transactions)
                .HasForeignKey(transaction => transaction.ImportBatchId);

            entity.HasIndex(transaction => transaction.ImportBatchId);
            entity.HasIndex(transaction => transaction.TransactionId);
            entity.HasIndex(transaction => transaction.Type);
            entity.HasIndex(transaction => new { transaction.ImportBatchId, transaction.TransactionId })
                .IsUnique();
        });

        modelBuilder.Entity<TransactionErrorRecord>(entity =>
        {
            entity.ToTable("TransactionErrors");
            entity.HasKey(error => error.Id);

            entity.Property(error => error.ErrorMessage)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasOne(error => error.ImportBatch)
                .WithMany(batch => batch.Errors)
                .HasForeignKey(error => error.ImportBatchId);

            entity.HasIndex(error => error.ImportBatchId);
            entity.HasIndex(error => error.LineNumber);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Exchange)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(message => message.RoutingKey)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(message => message.MessageKey)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(message => message.MessageType)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(message => message.Payload)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(message => message.LockId)
                .HasMaxLength(100);

            entity.Property(message => message.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(message => new { message.ProcessedAtUtc, message.LockedUntilUtc, message.OccurredAtUtc });
            entity.HasIndex(message => new { message.Exchange, message.RoutingKey, message.MessageKey });
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(message => message.Id);

            entity.Property(message => message.MessageId)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(message => message.Consumer)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(message => message.Exchange)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(message => message.RoutingKey)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(message => message.MessageType)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(message => message.Payload)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(message => message.LockId)
                .HasMaxLength(100);

            entity.Property(message => message.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(message => new { message.MessageId, message.Consumer })
                .IsUnique();
            entity.HasIndex(message => new { message.ProcessedAtUtc, message.LockedUntilUtc, message.ReceivedAtUtc });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case ImportBatch batch:
                    ApplyAudit(batch, entry.State, utcNow, batch.CreatedAtUtc, value => batch.CreatedAtUtc = value, value => batch.UpdatedAtUtc = value);
                    break;
                case TransactionRecord transaction:
                    ApplyAudit(transaction, entry.State, utcNow, transaction.CreatedAtUtc, value => transaction.CreatedAtUtc = value, value => transaction.UpdatedAtUtc = value);
                    break;
                case TransactionErrorRecord error:
                    if (entry.State == EntityState.Added && error.CreatedAtUtc == default)
                    {
                        error.CreatedAtUtc = utcNow;
                    }
                    break;
                case InboxMessage inboxMessage:
                    if (entry.State == EntityState.Added && inboxMessage.ReceivedAtUtc == default)
                    {
                        inboxMessage.ReceivedAtUtc = utcNow;
                    }
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyAudit<T>(
        T entity,
        EntityState state,
        DateTime utcNow,
        DateTime createdAtUtc,
        Action<DateTime> setCreatedAtUtc,
        Action<DateTime?> setUpdatedAtUtc)
    {
        if (state == EntityState.Added && createdAtUtc == default)
        {
            setCreatedAtUtc(utcNow);
        }

        if (state == EntityState.Modified)
        {
            setUpdatedAtUtc(utcNow);
        }
    }
}
