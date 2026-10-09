using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddWeighingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeighingRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WeightKg = table.Column<double>(type: "float", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeighingRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeighingRecords_StationKey_RecordedAtUtc",
                table: "WeighingRecords",
                columns: new[] { "StationKey", "RecordedAtUtc" });

            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM Plants WHERE [Key] = 'extruder2out') " +
                "INSERT INTO Plants ([Key], Name, Title, ViewName, SortOrder, IsActive) " +
                "VALUES ('extruder2out', N'Extruder #2 OUT', N'EXTRUDER WEIGHT MONITORING', 'ExtruderWeight', 4, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Plants WHERE [Key] = 'extruder2out'");

            migrationBuilder.DropTable(
                name: "WeighingRecords");
        }
    }
}
