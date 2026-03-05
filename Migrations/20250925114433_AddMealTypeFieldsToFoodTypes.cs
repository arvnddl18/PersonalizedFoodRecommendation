using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddMealTypeFieldsToFoodTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBeverage",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBreakfast",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDessert",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDinner",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLunch",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSnacks",
                table: "FoodTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBeverage",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsBreakfast",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsDessert",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsDinner",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsLunch",
                table: "FoodTypes");

            migrationBuilder.DropColumn(
                name: "IsSnacks",
                table: "FoodTypes");
        }
    }
}
