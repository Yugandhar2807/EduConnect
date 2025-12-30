using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizQuestionTypeAndDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "QuestionType",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectOption",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(char),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "QuizQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuestionTypeEnum",
                table: "QuizQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionTypeEnum",
                table: "QuizQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionType",
                table: "QuizQuestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<char>(
                name: "CorrectOption",
                table: "QuizQuestions",
                type: "TEXT",
                nullable: false,
                defaultValue: '\0',
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
