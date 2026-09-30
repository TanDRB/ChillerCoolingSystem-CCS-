using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChillerCoolingSystem_CCS_.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZoneKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpcDevice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MachineParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    ParameterKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabelKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedParameterKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineParameters_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Machines",
                columns: new[] { "Id", "ImageUrl", "Key", "Location", "Name", "OpcDevice", "SortOrder", "Type", "ZoneKey" },
                values: new object[,]
                {
                    { 1, "/image/CoolingTower.png", "ct3", "Banbury #1,2 , Openmill #1,2,7,8", "CT #3", "CT-CL.BANBURY.CT3", 1, "Cooling Tower", null },
                    { 2, "/image/CoolingTower.png", "ct7", "Banbury #3 , Openmill #10~12", "CT #7", "CT-CL.BANBURY.CT7", 2, "Cooling Tower", null },
                    { 3, "/image/Chiller.png", "cl3", "Banbury #2", "Chiller #3", "CT-CL.BANBURY.CL3", 3, "Chiller", "ct3" },
                    { 4, "/image/Chiller.png", "cl5", "Banbury #1", "Chiller #5", "CT-CL.BANBURY.CL5", 4, "Chiller", "ct3" },
                    { 5, "/image/Chiller.png", "cl8", "Banbury #3", "Chiller #8", "CT-CL.BANBURY.CL8", 5, "Chiller", "ct7" },
                    { 6, "/image/Chiller.png", "cl9", "Banbury #3", "Chiller #9", "CT-CL.BANBURY.CL9", 6, "Chiller", "ct7" },
                    { 7, "/image/Chiller.png", "cl10", "Banbury #3", "Chiller #10", "CT-CL.BANBURY.CL10", 7, "Chiller", "ct7" }
                });

            migrationBuilder.InsertData(
                table: "MachineParameters",
                columns: new[] { "Id", "IconKey", "Kind", "LabelKey", "LinkedParameterKey", "MachineId", "ParameterKey", "SortOrder", "Unit" },
                values: new object[,]
                {
                    { 1, "temp-red", "Temperature", "tempIn", null, 1, "TempIn", 1, "°C" },
                    { 2, "pressure-teal", "Pressure", "preWater", null, 1, "PreWater", 2, null },
                    { 3, "temp-blue", "Temperature", "tempOut", null, 1, "TempOut", 3, "°C" },
                    { 4, "run-stop", "Status", "runStop", null, 1, "RunStop", 4, null },
                    { 5, "temp-gray", "Temperature", "tempAmbi", null, 1, "TempAmbi", 5, "°C" },
                    { 6, "fault", "Fault", "fault", null, 1, "Fault", 6, null },
                    { 7, "", "ErrorFlag", "", "TempOut", 1, "TempError1", 7, null },
                    { 8, "", "ErrorFlag", "", "PreWater", 1, "TempError2", 8, null },
                    { 9, "temp-red", "Temperature", "tempIn", null, 2, "TempIn", 1, "°C" },
                    { 10, "pressure-teal", "Pressure", "preWater", null, 2, "PreWater", 2, null },
                    { 11, "temp-blue", "Temperature", "tempOut", null, 2, "TempOut", 3, "°C" },
                    { 12, "run-stop", "Status", "runStop", null, 2, "RunStop", 4, null },
                    { 13, "temp-gray", "Temperature", "tempAmbi", null, 2, "TempAmbi", 5, "°C" },
                    { 14, "fault", "Fault", "fault", null, 2, "Fault", 6, null },
                    { 15, "", "ErrorFlag", "", "TempOut", 2, "TempError1", 7, null },
                    { 16, "", "ErrorFlag", "", "PreWater", 2, "TempError2", 8, null },
                    { 17, "temp-red", "Temperature", "tempOutWater", null, 3, "TempOut", 1, "°C" },
                    { 18, "run-stop", "Status", "runStop", null, 3, "RunStop", 2, null },
                    { 19, "fault", "Fault", "fault", null, 3, "Fault", 3, null },
                    { 20, "temp-red", "Temperature", "tempOutWater", null, 4, "TempOut", 1, "°C" },
                    { 21, "run-stop", "Status", "runStop", null, 4, "RunStop", 2, null },
                    { 22, "fault", "Fault", "fault", null, 4, "Fault", 3, null },
                    { 23, "run-stop", "Status", "runStop", null, 5, "RunStop", 1, null },
                    { 24, "fault", "Fault", "fault", null, 5, "Fault", 2, null },
                    { 25, "run-stop", "Status", "runStop", null, 6, "RunStop", 1, null },
                    { 26, "fault", "Fault", "fault", null, 6, "Fault", 2, null },
                    { 27, "temp-red", "Temperature", "tempOutWater", null, 7, "TempOut", 1, "°C" },
                    { 28, "run-stop", "Status", "runStop", null, 7, "RunStop", 2, null },
                    { 29, "fault", "Fault", "fault", null, 7, "Fault", 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachineParameters_MachineId",
                table: "MachineParameters",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Key",
                table: "Machines",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineParameters");

            migrationBuilder.DropTable(
                name: "Machines");
        }
    }
}
