using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Portal.Migrations
{
    public partial class AddElective : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Electives",
                columns: table => new
                {
                    ElectiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Electives", x => x.ElectiveID);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveTypes",
                columns: table => new
                {
                    ElectiveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Archive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveTypes", x => x.ElectiveTypeID);
                });

            migrationBuilder.CreateTable(
                name: "El_Stud_Links",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectiveID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_El_Stud_Links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_El_Stud_Links_Electives_ElectiveID",
                        column: x => x.ElectiveID,
                        principalTable: "Electives",
                        principalColumn: "ElectiveID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveThemes",
                columns: table => new
                {
                    ElectiveThemeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Archive = table.Column<bool>(type: "bit", nullable: false),
                    ElectiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveThemes", x => x.ElectiveThemeID);
                    table.ForeignKey(
                        name: "FK_ElectiveThemes_Electives_ElectiveID",
                        column: x => x.ElectiveID,
                        principalTable: "Electives",
                        principalColumn: "ElectiveID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveLessons",
                columns: table => new
                {
                    ElectiveLessonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: false),
                    ElectiveThemeID = table.Column<int>(type: "int", nullable: false),
                    ElectiveTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveLessons", x => x.ElectiveLessonID);
                    table.ForeignKey(
                        name: "FK_ElectiveLessons_ElectiveThemes_ElectiveThemeID",
                        column: x => x.ElectiveThemeID,
                        principalTable: "ElectiveThemes",
                        principalColumn: "ElectiveThemeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectiveLessons_ElectiveTypes_ElectiveTypeID",
                        column: x => x.ElectiveTypeID,
                        principalTable: "ElectiveTypes",
                        principalColumn: "ElectiveTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveMarks",
                columns: table => new
                {
                    ElectiveMarkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureOfTeacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    ElectiveLessonID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveMarks", x => x.ElectiveMarkID);
                    table.ForeignKey(
                        name: "FK_ElectiveMarks_ElectiveLessons_ElectiveLessonID",
                        column: x => x.ElectiveLessonID,
                        principalTable: "ElectiveLessons",
                        principalColumn: "ElectiveLessonID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_El_Stud_Links_ElectiveID",
                table: "El_Stud_Links",
                column: "ElectiveID");

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveLessons_Date_FlagF",
                table: "ElectiveLessons",
                columns: new[] { "Date", "FlagF" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveLessons_ElectiveThemeID",
                table: "ElectiveLessons",
                column: "ElectiveThemeID");

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveLessons_ElectiveTypeID",
                table: "ElectiveLessons",
                column: "ElectiveTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveMarks_Date_FlagF",
                table: "ElectiveMarks",
                columns: new[] { "Date", "FlagF" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveMarks_ElectiveLessonID",
                table: "ElectiveMarks",
                column: "ElectiveLessonID");

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveThemes_ElectiveID",
                table: "ElectiveThemes",
                column: "ElectiveID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "El_Stud_Links");

            migrationBuilder.DropTable(
                name: "ElectiveMarks");

            migrationBuilder.DropTable(
                name: "ElectiveLessons");

            migrationBuilder.DropTable(
                name: "ElectiveThemes");

            migrationBuilder.DropTable(
                name: "ElectiveTypes");

            migrationBuilder.DropTable(
                name: "Electives");
        }
    }
}
