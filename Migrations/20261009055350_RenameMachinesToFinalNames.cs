using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class RenameMachinesToFinalNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration AddMachineCatalog đã chèn tên cũ ("CT #3", "Chiller #5"...) còn MachineCatalogSeeder (có tên mới) chỉ chạy
            // khi bảng Machines trống nên không bao giờ chạy trên DB đã migrate. Đồng nhất tên cho mọi DB (idempotent):
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Cooling Tower #3' WHERE [Key] = 'ct3'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Cooling Tower #7' WHERE [Key] = 'ct7'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Chiller #15' WHERE [Key] = 'cl5'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Machines SET Name = N'CT #3' WHERE [Key] = 'ct3'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'CT #7' WHERE [Key] = 'ct7'");
            migrationBuilder.Sql("UPDATE Machines SET Name = N'Chiller #5' WHERE [Key] = 'cl5'");
        }
    }
}
