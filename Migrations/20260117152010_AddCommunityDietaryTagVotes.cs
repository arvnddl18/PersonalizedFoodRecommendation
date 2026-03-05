using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityDietaryTagVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityRestaurants",
                columns: table => new
                {
                    PlaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    PriceLevel = table.Column<int>(type: "int", nullable: true),
                    PhotoReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Types = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRestaurants", x => x.PlaceId);
                });

            migrationBuilder.CreateTable(
                name: "CommunityDietaryTagVotes",
                columns: table => new
                {
                    VoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DietaryRestrictionId = table.Column<int>(type: "int", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityDietaryTagVotes", x => x.VoteId);
                    table.ForeignKey(
                        name: "FK_CommunityDietaryTagVotes_CommunityRestaurants_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "CommunityRestaurants",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityDietaryTagVotes_DietaryRestrictions_DietaryRestrictionId",
                        column: x => x.DietaryRestrictionId,
                        principalTable: "DietaryRestrictions",
                        principalColumn: "DietaryRestrictionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityDietaryTagVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityDietaryTagVotes_DietaryRestrictionId",
                table: "CommunityDietaryTagVotes",
                column: "DietaryRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityDietaryTagVotes_PlaceId_DietaryRestrictionId",
                table: "CommunityDietaryTagVotes",
                columns: new[] { "PlaceId", "DietaryRestrictionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityDietaryTagVotes_UserId_PlaceId_DietaryRestrictionId",
                table: "CommunityDietaryTagVotes",
                columns: new[] { "UserId", "PlaceId", "DietaryRestrictionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityDietaryTagVotes");

            migrationBuilder.DropTable(
                name: "CommunityRestaurants");
        }
    }
}
