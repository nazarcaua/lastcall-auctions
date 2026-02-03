using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastCallMotorAuctions.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleYearMakeModelHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleYears",
                columns: table => new
                {
                    Year = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleYears", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "VehicleYearMakes",
                columns: table => new
                {
                    YearMakeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    MakeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleYearMakes", x => x.YearMakeId);
                    table.ForeignKey(
                        name: "FK_VehicleYearMakes_VehicleMakes_MakeId",
                        column: x => x.MakeId,
                        principalTable: "VehicleMakes",
                        principalColumn: "MakeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleYearMakes_VehicleYears_Year",
                        column: x => x.Year,
                        principalTable: "VehicleYears",
                        principalColumn: "Year",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleYearMakeModels",
                columns: table => new
                {
                    YearMakeModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YearMakeId = table.Column<int>(type: "int", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleYearMakeModels", x => x.YearMakeModelId);
                    table.ForeignKey(
                        name: "FK_VehicleYearMakeModels_VehicleModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "VehicleModels",
                        principalColumn: "ModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleYearMakeModels_VehicleYearMakes_YearMakeId",
                        column: x => x.YearMakeId,
                        principalTable: "VehicleYearMakes",
                        principalColumn: "YearMakeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYearMakeModels_ModelId",
                table: "VehicleYearMakeModels",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYearMakeModels_YearMakeId_ModelId",
                table: "VehicleYearMakeModels",
                columns: new[] { "YearMakeId", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYearMakes_MakeId",
                table: "VehicleYearMakes",
                column: "MakeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleYearMakes_Year_MakeId",
                table: "VehicleYearMakes",
                columns: new[] { "Year", "MakeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleYearMakeModels");

            migrationBuilder.DropTable(
                name: "VehicleYearMakes");

            migrationBuilder.DropTable(
                name: "VehicleYears");
        }
    }
}
