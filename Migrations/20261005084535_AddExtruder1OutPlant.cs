using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddExtruder1OutPlant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thứ tự dropdown: Banbury, #1 IN, #1 OUT, #2 IN, #2 OUT.
            migrationBuilder.Sql("UPDATE Plants SET SortOrder = 4 WHERE [Key] = 'extruder2'");
            migrationBuilder.Sql("UPDATE Plants SET SortOrder = 5 WHERE [Key] = 'extruder2out'");
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM Plants WHERE [Key] = 'extruder1out') " +
                "INSERT INTO Plants ([Key], Name, Title, ViewName, SortOrder, IsActive) " +
                "VALUES ('extruder1out', N'Extruder #1 OUT', N'EXTRUDER WEIGHT MONITORING', 'ExtruderWeight', 3, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Plants WHERE [Key] = 'extruder1out'");
            migrationBuilder.Sql("UPDATE Plants SET SortOrder = 3 WHERE [Key] = 'extruder2'");
            migrationBuilder.Sql("UPDATE Plants SET SortOrder = 4 WHERE [Key] = 'extruder2out'");
        }
    }
}
