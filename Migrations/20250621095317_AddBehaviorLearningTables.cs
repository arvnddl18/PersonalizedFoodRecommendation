using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorLearningTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EstablishmentId = table.Column<int>(type: "int", nullable: false),
                    RecommendedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserRating = table.Column<int>(type: "int", nullable: true),
                    ReasonForRecommendation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserFeedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_RecommendationHistories_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "EstablishmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviors",
                columns: table => new
                {
                    BehaviorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Satisfaction = table.Column<int>(type: "int", nullable: true),
                    IntentDetected = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParametersJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviors", x => x.BehaviorId);
                    table.ForeignKey(
                        name: "FK_UserBehaviors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferencePatterns",
                columns: table => new
                {
                    PatternId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PatternType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PatternValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    LastObserved = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObservationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferencePatterns", x => x.PatternId);
                    table.ForeignKey(
                        name: "FK_UserPreferencePatterns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistories_EstablishmentId",
                table: "RecommendationHistories",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistories_UserId",
                table: "RecommendationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviors_UserId",
                table: "UserBehaviors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferencePatterns_UserId",
                table: "UserPreferencePatterns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationHistories");

            migrationBuilder.DropTable(
                name: "UserBehaviors");

            migrationBuilder.DropTable(
                name: "UserPreferencePatterns");
        }
    }
}
