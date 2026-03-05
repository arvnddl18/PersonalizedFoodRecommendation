using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class FinalDatabaseCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDietaryRestrictions_UserProfiles_UserId",
                table: "UserDietaryRestrictions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHealthConditions_UserProfiles_UserId",
                table: "UserHealthConditions");

            migrationBuilder.DropTable(
                name: "EstablishmentFoodTypes");

            migrationBuilder.DropTable(
                name: "Establishments");

            migrationBuilder.DropColumn(
                name: "DietaryPreference",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FavoriteFood",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "PriceRange",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PreferredLocation",
                table: "UserProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PriceRange",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PreferredLocation",
                table: "UserProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietaryPreference",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FavoriteFood",
                table: "UserProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Establishments",
                columns: table => new
                {
                    EstablishmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CuisineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Establishments", x => x.EstablishmentId);
                });

            migrationBuilder.CreateTable(
                name: "EstablishmentFoodTypes",
                columns: table => new
                {
                    EstablishmentId = table.Column<int>(type: "int", nullable: false),
                    SupportedFoodTypesFoodTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstablishmentFoodTypes", x => new { x.EstablishmentId, x.SupportedFoodTypesFoodTypeId });
                    table.ForeignKey(
                        name: "FK_EstablishmentFoodTypes_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "EstablishmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstablishmentFoodTypes_FoodTypes_SupportedFoodTypesFoodTypeId",
                        column: x => x.SupportedFoodTypesFoodTypeId,
                        principalTable: "FoodTypes",
                        principalColumn: "FoodTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentFoodTypes_SupportedFoodTypesFoodTypeId",
                table: "EstablishmentFoodTypes",
                column: "SupportedFoodTypesFoodTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDietaryRestrictions_UserProfiles_UserId",
                table: "UserDietaryRestrictions",
                column: "UserId",
                principalTable: "UserProfiles",
                principalColumn: "ProfileId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHealthConditions_UserProfiles_UserId",
                table: "UserHealthConditions",
                column: "UserId",
                principalTable: "UserProfiles",
                principalColumn: "ProfileId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
