using Krik.Api.Entities;
using Krik.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Roles.AnyAsync(cancellationToken))
        {
        var roleAdmin = new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = KrikRoles.AdminHR };
        var roleArea = new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = KrikRoles.AreaManager };
        var roleStore = new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = KrikRoles.StoreManager };
        var roleSales = new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Name = KrikRoles.SalesStaff };
        db.Roles.AddRange(roleAdmin, roleArea, roleStore, roleSales);

        var area = new Area
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
            Code = "BNB",
            Name = "Khu BNB"
        };
        db.Areas.Add(area);

        var storeK01 = new Store
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            AreaId = area.Id,
            Code = "K01",
            Name = "Cửa hàng K01"
        };
        var storeK02 = new Store
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
            AreaId = area.Id,
            Code = "K02",
            Name = "Cửa hàng K02"
        };
        db.Stores.AddRange(storeK01, storeK02);

        const string devPassword = "Admin123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(devPassword);

        var admin = new KrikUser
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            Email = "admin@krik.local",
            PasswordHash = hash,
            FullName = "Admin HR",
            StoreId = null
        };
        admin.UserRoles.Add(new UserRole { User = admin, Role = roleAdmin });

        var areaMgr = new KrikUser
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
            Email = "area@krik.local",
            PasswordHash = hash,
            FullName = "Area Manager",
            StoreId = null
        };
        areaMgr.UserRoles.Add(new UserRole { User = areaMgr, Role = roleArea });
        areaMgr.UserAreas.Add(new UserArea { User = areaMgr, Area = area });

        var storeMgr = new KrikUser
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444403"),
            Email = "store@krik.local",
            PasswordHash = hash,
            FullName = "QLCH K01",
            StoreId = storeK01.Id
        };
        storeMgr.UserRoles.Add(new UserRole { User = storeMgr, Role = roleStore });

        var sales = new KrikUser
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444404"),
            Email = "sales@krik.local",
            PasswordHash = hash,
            FullName = "NVBH K01",
            StoreId = storeK01.Id
        };
        sales.UserRoles.Add(new UserRole { User = sales, Role = roleSales });

        db.Users.AddRange(admin, areaMgr, storeMgr, sales);
        await db.SaveChangesAsync(cancellationToken);
        }

        await StaffShiftKpiDemoSeed.EnsureSeedAsync(db, cancellationToken);
    }
}
