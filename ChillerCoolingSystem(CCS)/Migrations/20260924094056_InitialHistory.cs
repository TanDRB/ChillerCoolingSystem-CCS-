using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class InitialHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Good = table.Column<bool>(type: "bit", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagHistoryEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_MachineKey_StartUtc",
                table: "AlarmEvents",
                columns: new[] { "MachineKey", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TagHistoryEntries_MachineKey_TimestampUtc",
                table: "TagHistoryEntries",
                columns: new[] { "MachineKey", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmEvents");

            migrationBuilder.DropTable(
                name: "TagHistoryEntries");
        }
    }
}
