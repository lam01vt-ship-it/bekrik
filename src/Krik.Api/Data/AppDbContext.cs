using Krik.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<KrikUser> Users => Set<KrikUser>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserArea> UserAreas => Set<UserArea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Area>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Store>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Area).WithMany(a => a.Stores).HasForeignKey(x => x.AreaId);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<KrikUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<UserArea>(e =>
        {
            e.HasKey(x => new { x.UserId, x.AreaId });
            e.HasOne(x => x.User).WithMany(u => u.UserAreas).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Area).WithMany(a => a.UserAreas).HasForeignKey(x => x.AreaId);
        });
    }
}
