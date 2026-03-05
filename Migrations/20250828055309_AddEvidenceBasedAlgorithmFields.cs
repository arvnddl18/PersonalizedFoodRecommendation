using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceBasedAlgorithmFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivityLevel",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulInteractions",
                table: "UserPreferencePatterns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInteractions",
                table: "UserPreferencePatterns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ContainsDairy",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContainsGluten",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContainsMeat",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ContainsNuts",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CuisineType",
                table: "FoodTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsHealthy",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHeartHealthy",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHighProtein",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIronRich",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLowCalorie",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLowGlycemic",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLowSodium",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNutrientDense",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegan",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegetarian",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVitaminRich",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SuccessfulInteractions",
                table: "UserPreferencePatterns");

            migrationBuilder.DropColumn(
                name: "TotalInteractions",
                table: "UserPreferencePatterns");

            migrationBuilder.DropColumn(
                name: "ContainsDairy",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "ContainsGluten",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "ContainsMeat",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "ContainsNuts",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "CuisineType",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsHealthy",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsHeartHealthy",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsHighProtein",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsIronRich",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsLowCalorie",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsLowGlycemic",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsLowSodium",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsNutrientDense",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsVegan",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsVegetarian",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsVitaminRich",
                table: "FoodTypes");
        }
    }
}
