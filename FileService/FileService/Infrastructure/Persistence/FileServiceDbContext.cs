using FileService.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace FileService.Infrastructure.Persistence;

public sealed class FileServiceDbContext(DbContextOptions<FileServiceDbContext> options) : DbContext(options)
{
    public DbSet<FileRecord> Files => Set<FileRecord>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(file => file.Id);

            entity.Property(file => file.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(file => file.StoredFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(file => file.StoragePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(file => file.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(file => file.FileExtension)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(file => file.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(file => file.ErrorMessage)
                .HasMaxLength(1000);

            entity.Property(file => file.UploadedAtUtc)
                .IsRequired();

            entity.HasIndex(file => file.StoredFileName)
                .IsUnique();

            entity.HasIndex(file => file.CorrelationId)
                .IsUnique()
                .HasFilter("[CorrelationId] IS NOT NULL");

            entity.HasIndex(file => file.UploadedByUserId);
            entity.HasIndex(file => file.Status);
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

        foreach (var entry in ChangeTracker.Entries<FileRecord>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                if (entry.Entity.UploadedAtUtc == default)
                {
                    entry.Entity.UploadedAtUtc = utcNow;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<InboxMessage>())
        {
            if (entry.State == EntityState.Added && entry.Entity.ReceivedAtUtc == default)
            {
                entry.Entity.ReceivedAtUtc = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
