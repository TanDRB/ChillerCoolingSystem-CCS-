using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddExtruder2Plant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM Plants WHERE [Key] = 'extruder2') " +
                "INSERT INTO Plants ([Key], Name, Title, ViewName, SortOrder, IsActive) " +
                "VALUES ('extruder2', N'Extruder #2 IN', N'EXTRUDER TEMPERATURE MONITORING', 'Extruder', 3, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Plants WHERE [Key] = 'extruder2'");
        }
    }
}
