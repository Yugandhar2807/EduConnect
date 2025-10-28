using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapTemplatesAndTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadmapTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EstimatedHours = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoadmapTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ParentTopicId = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionY = table.Column<int>(type: "INTEGER", nullable: false),
                    FreeResources = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PaidResources = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AITutorPrompt = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapTopics_RoadmapTemplates_RoadmapTemplateId",
                        column: x => x.RoadmapTemplateId,
                        principalTable: "RoadmapTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadmapTopics_RoadmapTopics_ParentTopicId",
                        column: x => x.ParentTopicId,
                        principalTable: "RoadmapTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentRoadmapProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<string>(type: "TEXT", nullable: false),
                    RoadmapTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedTopicIds = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRoadmapProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRoadmapProgress_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentRoadmapProgress_RoadmapTemplates_RoadmapTemplateId",
                        column: x => x.RoadmapTemplateId,
                        principalTable: "RoadmapTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapTopics_ParentTopicId",
                table: "RoadmapTopics",
                column: "ParentTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapTopics_RoadmapTemplateId",
                table: "RoadmapTopics",
                column: "RoadmapTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRoadmapProgress_RoadmapTemplateId",
                table: "StudentRoadmapProgress",
                column: "RoadmapTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRoadmapProgress_StudentId",
                table: "StudentRoadmapProgress",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadmapTopics");

            migrationBuilder.DropTable(
                name: "StudentRoadmapProgress");

            migrationBuilder.DropTable(
                name: "RoadmapTemplates");
        }
    }
}
