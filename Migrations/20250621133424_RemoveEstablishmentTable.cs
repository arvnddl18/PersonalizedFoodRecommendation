using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEstablishmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecommendationHistories_Establishments_EstablishmentId",
                table: "RecommendationHistories");

            migrationBuilder.DropTable(
                name: "Establishments");

            migrationBuilder.DropIndex(
                name: "IX_RecommendationHistories_EstablishmentId",
                table: "RecommendationHistories");

            migrationBuilder.DropColumn(
                name: "EstablishmentId",
                table: "RecommendationHistories");

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "UserBehaviors",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuisineType",
                table: "RecommendationHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstablishmentInfo",
                table: "RecommendationHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstablishmentName",
                table: "RecommendationHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceRange",
                table: "RecommendationHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuisineType",
                table: "RecommendationHistories");

            migrationBuilder.DropColumn(
                name: "EstablishmentInfo",
                table: "RecommendationHistories");

            migrationBuilder.DropColumn(
                name: "EstablishmentName",
                table: "RecommendationHistories");

            migrationBuilder.DropColumn(
                name: "PriceRange",
                table: "RecommendationHistories");

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "UserBehaviors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstablishmentId",
                table: "RecommendationHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Establishments",
                columns: table => new
                {
                    EstablishmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoodTypeId = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DietaryOptions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GoogleMapsPlaceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Establishments", x => x.EstablishmentId);
                    table.ForeignKey(
                        name: "FK_Establishments_FoodTypes_FoodTypeId",
                        column: x => x.FoodTypeId,
                        principalTable: "FoodTypes",
                        principalColumn: "FoodTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistories_EstablishmentId",
                table: "RecommendationHistories",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Establishments_FoodTypeId",
                table: "Establishments",
                column: "FoodTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecommendationHistories_Establishments_EstablishmentId",
                table: "RecommendationHistories",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "EstablishmentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
