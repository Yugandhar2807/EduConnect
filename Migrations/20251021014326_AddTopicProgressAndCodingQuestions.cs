using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicProgressAndCodingQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeTemplate",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutput",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammingLanguage",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionType",
                table: "QuizQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TopicProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<string>(type: "TEXT", nullable: false),
                    TopicId = table.Column<int>(type: "INTEGER", nullable: true),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicProgress_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicProgress_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicProgress_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgress_MaterialId",
                table: "TopicProgress",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgress_StudentId",
                table: "TopicProgress",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgress_TopicId",
                table: "TopicProgress",
                column: "TopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicProgress");

            migrationBuilder.DropColumn(
                name: "CodeTemplate",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "ExpectedOutput",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "ProgrammingLanguage",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "QuizQuestions");
        }
    }
}
