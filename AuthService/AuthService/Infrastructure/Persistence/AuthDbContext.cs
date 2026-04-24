using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Base;

namespace AuthService.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(role => role.Id);

            entity.Property(role => role.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(role => role.Description)
                .HasMaxLength(255);

            entity.HasIndex(role => role.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(permission => permission.Id);

            entity.Property(permission => permission.Code)
                .HasColumnName("Code")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(permission => permission.Name)
                .HasColumnName("Name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(permission => permission.Description)
                .HasColumnName("Description")
                .HasMaxLength(255);

            entity.HasIndex(permission => permission.Code)
                .IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId);

            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId);

            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            entity.HasKey(userPermission => new { userPermission.UserId, userPermission.PermissionId });

            entity.HasOne(userPermission => userPermission.User)
                .WithMany(user => user.UserPermissions)
                .HasForeignKey(userPermission => userPermission.UserId);

            entity.HasOne(userPermission => userPermission.Permission)
                .WithMany(permission => permission.UserPermissions)
                .HasForeignKey(userPermission => userPermission.PermissionId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = utcNow;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
