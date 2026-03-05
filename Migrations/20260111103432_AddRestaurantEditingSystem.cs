using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantEditingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantOwners",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RestaurantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOwners", x => x.OwnerId);
                    table.ForeignKey(
                        name: "FK_RestaurantOwners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantOwners_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantVersions",
                columns: table => new
                {
                    VersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CuisineType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceRange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OpeningHours = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SpecialFeatures = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantVersions", x => x.VersionId);
                    table.ForeignKey(
                        name: "FK_RestaurantVersions_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerVerifications",
                columns: table => new
                {
                    VerificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    BusinessLicensePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BusinessPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BusinessAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    PhoneVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerVerifications", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_OwnerVerifications_RestaurantOwners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "RestaurantOwners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantEdits",
                columns: table => new
                {
                    EditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FieldChanges = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantEdits", x => x.EditId);
                    table.ForeignKey(
                        name: "FK_RestaurantEdits_RestaurantVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "RestaurantVersions",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantEdits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerVerifications_OwnerId",
                table: "OwnerVerifications",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEdits_PlaceId",
                table: "RestaurantEdits",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEdits_UserId",
                table: "RestaurantEdits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEdits_VersionId",
                table: "RestaurantEdits",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOwners_PlaceId",
                table: "RestaurantOwners",
                column: "PlaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOwners_UserId",
                table: "RestaurantOwners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOwners_VerifiedByUserId",
                table: "RestaurantOwners",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantVersions_ApprovedByUserId",
                table: "RestaurantVersions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantVersions_CreatedByUserId",
                table: "RestaurantVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantVersions_PlaceId_IsCurrent",
                table: "RestaurantVersions",
                columns: new[] { "PlaceId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantVersions_Status",
                table: "RestaurantVersions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerVerifications");

            migrationBuilder.DropTable(
                name: "RestaurantEdits");

            migrationBuilder.DropTable(
                name: "RestaurantOwners");

            migrationBuilder.DropTable(
                name: "RestaurantVersions");
        }
    }
}
