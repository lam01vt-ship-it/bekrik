using Krik.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krik.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260513120000_StoreDailySummaryIsDayLocked")]
public partial class StoreDailySummaryIsDayLocked : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDayLocked",
            table: "StoreDailySummaries",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsDayLocked",
            table: "StoreDailySummaries");
    }
}
