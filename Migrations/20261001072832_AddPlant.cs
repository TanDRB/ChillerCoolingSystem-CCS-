using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddPlant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_Key",
                table: "Plants",
                column: "Key",
                unique: true);

            // Seed xưởng Banbury hiện tại trước khi gán PlantId cho 7 máy đã có.
            migrationBuilder.InsertData(
                table: "Plants",
                columns: new[] { "Id", "Key", "Name", "Title", "ViewName", "SortOrder", "IsActive" },
                values: new object[] { 1, "banbury", "Banbury - DRB 1", null, "Banbury", 1, true });

            // Thêm cột PlantId dạng nullable trước, backfill xong mới ép NOT NULL —
            // tránh EF phải chèn 1 defaultValue tạm thời (vd 0) không khớp Plant nào.
            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "Machines",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE Machines SET PlantId = 1 WHERE PlantId IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "Machines",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_PlantId",
                table: "Machines",
                column: "PlantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_Plants_PlantId",
                table: "Machines",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Plants_PlantId",
                table: "Machines");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_Machines_PlantId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "Machines");
        }
    }
}
