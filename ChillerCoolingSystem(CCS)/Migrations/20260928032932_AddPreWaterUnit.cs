using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddPreWaterUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MachineParameters",
                keyColumn: "Id",
                keyValue: 2,
                column: "Unit",
                value: "kg/cm²");

            migrationBuilder.UpdateData(
                table: "MachineParameters",
                keyColumn: "Id",
                keyValue: 10,
                column: "Unit",
                value: "kg/cm²");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MachineParameters",
                keyColumn: "Id",
                keyValue: 2,
                column: "Unit",
                value: null);

            migrationBuilder.UpdateData(
                table: "MachineParameters",
                keyColumn: "Id",
                keyValue: 10,
                column: "Unit",
                value: null);
        }
    }
}
