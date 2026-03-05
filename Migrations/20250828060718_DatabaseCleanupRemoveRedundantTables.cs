using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseCleanupRemoveRedundantTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationHistories");

            migrationBuilder.DropColumn(
                name: "LastKnownLatitude",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastKnownLongitude",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "UserBehaviors",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CuisineType",
                table: "UserBehaviors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstablishmentInfo",
                table: "UserBehaviors",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstablishmentName",
                table: "UserBehaviors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForRecommendation",
                table: "UserBehaviors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SelectedAt",
                table: "UserBehaviors",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuisineType",
                table: "UserBehaviors");

            migrationBuilder.DropColumn(
                name: "EstablishmentInfo",
                table: "UserBehaviors");

            migrationBuilder.DropColumn(
                name: "EstablishmentName",
                table: "UserBehaviors");

            migrationBuilder.DropColumn(
                name: "ReasonForRecommendation",
                table: "UserBehaviors");

            migrationBuilder.DropColumn(
                name: "SelectedAt",
                table: "UserBehaviors");

            migrationBuilder.AddColumn<decimal>(
                name: "LastKnownLatitude",
                table: "UserProfiles",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastKnownLongitude",
                table: "UserProfiles",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "UserBehaviors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateTable(
                name: "RecommendationHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CuisineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstablishmentInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstablishmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PriceRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReasonForRecommendation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecommendedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserFeedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserRating = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_RecommendationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistories_UserId",
                table: "RecommendationHistories",
                column: "UserId");
        }
    }
}
