using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Migrations
{
    /// <inheritdoc />
    public partial class CleanupRoadmaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadmapNodes");

            migrationBuilder.DropTable(
                name: "RoadmapTopics");

            migrationBuilder.DropTable(
                name: "StudentRoadmapProgress");

            migrationBuilder.DropTable(
                name: "RoadmapTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadmapNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudentId = table.Column<string>(type: "TEXT", nullable: true),
                    TopicId = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapNodes_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadmapNodes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoadmapNodes_RoadmapNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RoadmapNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadmapNodes_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoadmapTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EstimatedHours = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
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
                    ParentTopicId = table.Column<int>(type: "INTEGER", nullable: true),
                    RoadmapTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    AITutorPrompt = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FreeResources = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidResources = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PositionX = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionY = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
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
                    RoadmapTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedTopicIds = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_RoadmapNodes_CourseId",
                table: "RoadmapNodes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapNodes_ParentId",
                table: "RoadmapNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapNodes_StudentId",
                table: "RoadmapNodes",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapNodes_TopicId",
                table: "RoadmapNodes",
                column: "TopicId");

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
    }
}
