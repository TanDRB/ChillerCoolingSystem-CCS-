using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddExtruderPlant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM Plants WHERE [Key] = 'extruder') " +
                "INSERT INTO Plants ([Key], Name, Title, ViewName, SortOrder, IsActive) " +
                "VALUES ('extruder', N'Extruder #1 IN', N'EXTRUDER TEMPERATURE MONITORING', 'Extruder', 2, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Plants WHERE [Key] = 'extruder'");
        }
    }
}
