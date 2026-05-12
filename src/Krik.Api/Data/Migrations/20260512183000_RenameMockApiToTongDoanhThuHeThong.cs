using Krik.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krik.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260512183000_RenameMockApiToTongDoanhThuHeThong")]
public partial class RenameMockApiToTongDoanhThuThong : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "MockApiRevenueTotal",
            table: "StoreDailySummaries",
            newName: "TongDoanhThuHeThong");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "TongDoanhThuHeThong",
            table: "StoreDailySummaries",
            newName: "MockApiRevenueTotal");
    }
}
