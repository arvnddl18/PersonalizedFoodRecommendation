using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CuisineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacebookPageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FacebookAbout = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FacebookDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FacebookWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacebookPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FacebookCoverPhoto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacebookProfilePicture = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacebookRating = table.Column<double>(type: "float", nullable: true),
                    FacebookRatingCount = table.Column<int>(type: "int", nullable: true),
                    FacebookCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FacebookHours = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FacebookPriceRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FacebookRecentPosts = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FacebookDataLastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.RestaurantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Restaurants");
        }
    }
}
