using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ChillerCoolingSystem_CCS_.Migrations
{
    public partial class RenameMachinesToFinalNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddMachineCatalog đã chèn tên cũ ("CT #3", "Chiller #5"); đồng nhất tên cho mọi DB (chạy lại vẫn an toàn).
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Cooling Tower #3' WHERE [Key] = 'ct3'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Cooling Tower #7' WHERE [Key] = 'ct7'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Chiller #15' WHERE [Key] = 'cl5'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Machines SET Name = N'CT #3' WHERE [Key] = 'ct3'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'CT #7' WHERE [Key] = 'ct7'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Chiller #5' WHERE [Key] = 'cl5'");
        }
    }
}
