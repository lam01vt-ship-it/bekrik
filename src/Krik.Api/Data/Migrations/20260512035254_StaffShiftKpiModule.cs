using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krik.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StaffShiftKpiModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommissionBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionCode = table.Column<string>(type: "text", nullable: false),
                    ContractType = table.Column<string>(type: "text", nullable: false),
                    KpiPctMin = table.Column<decimal>(type: "numeric", nullable: false),
                    KpiPctMax = table.Column<decimal>(type: "numeric", nullable: true),
                    CommissionPct = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreDailySummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ChannelRevenueMorning = table.Column<decimal>(type: "numeric", nullable: false),
                    ChannelRevenueAfternoon = table.Column<decimal>(type: "numeric", nullable: false),
                    ChannelRevenueEvening = table.Column<decimal>(type: "numeric", nullable: false),
                    StoreCustomers = table.Column<int>(type: "integer", nullable: false),
                    StoreOrders = table.Column<int>(type: "integer", nullable: false),
                    StoreProducts = table.Column<int>(type: "integer", nullable: false),
                    StoreDayKpiTarget = table.Column<decimal>(type: "numeric", nullable: false),
                    MockApiRevenueTotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreDailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreDailySummaries_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreMonthlyKpiConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthlyTargetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    WeekRatiosJson = table.Column<string>(type: "text", nullable: false),
                    DayRatiosJson = table.Column<string>(type: "text", nullable: false),
                    ShiftRatiosJson = table.Column<string>(type: "text", nullable: false),
                    IsMonthLocked = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreMonthlyKpiConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreMonthlyKpiConfigs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreStaff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PositionCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ContractType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TeamBonusBase = table.Column<decimal>(type: "numeric", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreStaff_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreStaff_Users_LinkedUserId",
                        column: x => x.LinkedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StaffDailyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HoursMorning = table.Column<decimal>(type: "numeric", nullable: false),
                    HoursAfternoon = table.Column<decimal>(type: "numeric", nullable: false),
                    HoursEvening = table.Column<decimal>(type: "numeric", nullable: false),
                    HoursExtra = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueMorning = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueAfternoon = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueEvening = table.Column<decimal>(type: "numeric", nullable: false),
                    Customers = table.Column<int>(type: "integer", nullable: false),
                    TryOns = table.Column<int>(type: "integer", nullable: false),
                    Orders = table.Column<int>(type: "integer", nullable: false),
                    Products = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDailyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffDailyEntries_StoreStaff_StoreStaffId",
                        column: x => x.StoreStaffId,
                        principalTable: "StoreStaff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionBrackets_PositionCode_ContractType_EffectiveFrom",
                table: "CommissionBrackets",
                columns: new[] { "PositionCode", "ContractType", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyEntries_StoreStaffId_WorkDate",
                table: "StaffDailyEntries",
                columns: new[] { "StoreStaffId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreDailySummaries_StoreId_WorkDate",
                table: "StoreDailySummaries",
                columns: new[] { "StoreId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreMonthlyKpiConfigs_StoreId_YearMonth",
                table: "StoreMonthlyKpiConfigs",
                columns: new[] { "StoreId", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreStaff_LinkedUserId",
                table: "StoreStaff",
                column: "LinkedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreStaff_StoreId_StaffCode",
                table: "StoreStaff",
                columns: new[] { "StoreId", "StaffCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionBrackets");

            migrationBuilder.DropTable(
                name: "StaffDailyEntries");

            migrationBuilder.DropTable(
                name: "StoreDailySummaries");

            migrationBuilder.DropTable(
                name: "StoreMonthlyKpiConfigs");

            migrationBuilder.DropTable(
                name: "StoreStaff");
        }
    }
}
