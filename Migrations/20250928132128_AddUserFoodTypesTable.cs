using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFoodTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFoodTypes",
                columns: table => new
                {
                    UserFoodTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FoodTypeId = table.Column<int>(type: "int", nullable: false),
                    PreferenceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreferenceScore = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSelected = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFoodTypes", x => x.UserFoodTypeId);
                    table.ForeignKey(
                        name: "FK_UserFoodTypes_FoodTypes_FoodTypeId",
                        column: x => x.FoodTypeId,
                        principalTable: "FoodTypes",
                        principalColumn: "FoodTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFoodTypes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFoodTypes_FoodTypeId",
                table: "UserFoodTypes",
                column: "FoodTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFoodTypes_UserId",
                table: "UserFoodTypes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFoodTypes");
        }
    }
}
