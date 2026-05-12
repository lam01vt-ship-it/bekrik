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

    public DbSet<StoreStaff> StoreStaff => Set<StoreStaff>();
    public DbSet<StaffDailyEntry> StaffDailyEntries => Set<StaffDailyEntry>();
    public DbSet<StoreDailySummary> StoreDailySummaries => Set<StoreDailySummary>();
    public DbSet<StoreMonthlyKpiConfig> StoreMonthlyKpiConfigs => Set<StoreMonthlyKpiConfig>();
    public DbSet<CommissionBracket> CommissionBrackets => Set<CommissionBracket>();

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

        modelBuilder.Entity<StoreStaff>(e =>
        {
            e.HasIndex(x => new { x.StoreId, x.StaffCode }).IsUnique();
            e.Property(x => x.StaffCode).HasMaxLength(64);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.PositionCode).HasMaxLength(32);
            e.Property(x => x.ContractType).HasMaxLength(8);
            e.HasOne(x => x.Store).WithMany(s => s.StaffMembers).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StaffDailyEntry>(e =>
        {
            e.HasIndex(x => new { x.StoreStaffId, x.WorkDate }).IsUnique();
            e.HasOne(x => x.StoreStaff).WithMany(s => s.DailyEntries).HasForeignKey(x => x.StoreStaffId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreDailySummary>(e =>
        {
            e.HasIndex(x => new { x.StoreId, x.WorkDate }).IsUnique();
            e.HasOne(x => x.Store).WithMany(s => s.DailySummaries).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreMonthlyKpiConfig>(e =>
        {
            e.HasIndex(x => new { x.StoreId, x.YearMonth }).IsUnique();
            e.HasOne(x => x.Store).WithMany(s => s.MonthlyKpiConfigs).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommissionBracket>(e =>
        {
            e.HasIndex(x => new { x.PositionCode, x.ContractType, x.EffectiveFrom });
        });
    }
}
