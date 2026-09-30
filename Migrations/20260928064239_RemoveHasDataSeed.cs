using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHasDataSeed : Migration
    {
        // Migration này chỉ đánh dấu model không còn seed qua HasData nữa (xem
        // MachineCatalogSeeder.SeedAsync — seed lúc khởi động thay vì trong migration).
        // Không có thay đổi schema hay dữ liệu thật: Up()/Down() để trống có chủ đích,
        // KHÔNG dùng migrationBuilder.DeleteData tự sinh — nếu không sẽ xoá mất dữ liệu
        // 7 máy/29 thông số đã có sẵn trên DB thật (đã seed qua các migration cũ).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
