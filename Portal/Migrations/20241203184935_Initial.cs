using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Portal.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Arch = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstituteID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "Electives",
                columns: table => new
                {
                    ElectiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Log = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Teacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                });

            migrationBuilder.CreateTable(
                name: "Institutes",
                columns: table => new
                {
                    InstituteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Arch = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutes", x => x.InstituteID);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    File = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.ScheduleID);
                });

            migrationBuilder.CreateTable(
                name: "Sub_SpecLinks",
                columns: table => new
                {
                    LinkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialityID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sub_SpecLinks", x => x.LinkID);
                });

            migrationBuilder.CreateTable(
                name: "TeacherNoPCs",
                columns: table => new
                {
                    TeacherNoPCID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherNoPCs", x => x.TeacherNoPCID);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherID);
                });

            migrationBuilder.CreateTable(
                name: "TypeOfExercise",
                columns: table => new
                {
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Arch = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeOfExercise", x => x.TypeOfExerciseID);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Arch = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectID);
                    table.ForeignKey(
                        name: "FK_Subjects_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Cascade);
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
                name: "Specialities",
                columns: table => new
                {
                    SpecialityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeOFStudy = table.Column<int>(type: "int", nullable: false),
                    Arch = table.Column<bool>(type: "bit", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialities", x => x.SpecialityID);
                    table.ForeignKey(
                        name: "FK_Specialities_Institutes_InstituteID",
                        column: x => x.InstituteID,
                        principalTable: "Institutes",
                        principalColumn: "InstituteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    ThemeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Arch = table.Column<bool>(type: "bit", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.ThemeID);
                    table.ForeignKey(
                        name: "FK_Themes_Subjects_SubjectID",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
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
                name: "GroupArhives",
                columns: table => new
                {
                    GroupArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateEnter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateExit = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupArhives", x => x.GroupArhiveID);
                    table.ForeignKey(
                        name: "FK_GroupArhives_Specialities_SpecialityID",
                        column: x => x.SpecialityID,
                        principalTable: "Specialities",
                        principalColumn: "SpecialityID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateEnter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateExit = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupID);
                    table.ForeignKey(
                        name: "FK_Groups_Specialities_SpecialityID",
                        column: x => x.SpecialityID,
                        principalTable: "Specialities",
                        principalColumn: "SpecialityID",
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

            migrationBuilder.CreateTable(
                name: "JournalArhives",
                columns: table => new
                {
                    JournalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupArhiveID = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalArhives", x => x.JournalID);
                    table.ForeignKey(
                        name: "FK_JournalArhives_GroupArhives_GroupArhiveID",
                        column: x => x.GroupArhiveID,
                        principalTable: "GroupArhives",
                        principalColumn: "GroupArhiveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalArhives_Subjects_SubjectID",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonArhives",
                columns: table => new
                {
                    LessonArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false),
                    ThemeID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    GroupArhiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonArhives", x => x.LessonArhiveID);
                    table.ForeignKey(
                        name: "FK_LessonArhives_GroupArhives_GroupArhiveID",
                        column: x => x.GroupArhiveID,
                        principalTable: "GroupArhives",
                        principalColumn: "GroupArhiveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonArhives_Themes_ThemeID",
                        column: x => x.ThemeID,
                        principalTable: "Themes",
                        principalColumn: "ThemeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonArhives_TypeOfExercise_TypeOfExerciseID",
                        column: x => x.TypeOfExerciseID,
                        principalTable: "TypeOfExercise",
                        principalColumn: "TypeOfExerciseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatementLessonArhives",
                columns: table => new
                {
                    StatementLessonArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    GroupArhiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementLessonArhives", x => x.StatementLessonArhiveID);
                    table.ForeignKey(
                        name: "FK_StatementLessonArhives_GroupArhives_GroupArhiveID",
                        column: x => x.GroupArhiveID,
                        principalTable: "GroupArhives",
                        principalColumn: "GroupArhiveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatementLessonArhives_TypeOfExercise_TypeOfExerciseID",
                        column: x => x.TypeOfExerciseID,
                        principalTable: "TypeOfExercise",
                        principalColumn: "TypeOfExerciseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentArhives",
                columns: table => new
                {
                    StudentArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlaceOfBirth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    GroupArhiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentArhives", x => x.StudentArhiveID);
                    table.ForeignKey(
                        name: "FK_StudentArhives_GroupArhives_GroupArhiveID",
                        column: x => x.GroupArhiveID,
                        principalTable: "GroupArhives",
                        principalColumn: "GroupArhiveID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Journals",
                columns: table => new
                {
                    JournalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journals", x => x.JournalID);
                    table.ForeignKey(
                        name: "FK_Journals_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Journals_Subjects_SubjectID",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false),
                    ThemeID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.LessonID);
                    table.ForeignKey(
                        name: "FK_Lessons_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lessons_Themes_ThemeID",
                        column: x => x.ThemeID,
                        principalTable: "Themes",
                        principalColumn: "ThemeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lessons_TypeOfExercise_TypeOfExerciseID",
                        column: x => x.TypeOfExerciseID,
                        principalTable: "TypeOfExercise",
                        principalColumn: "TypeOfExerciseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatementLessons",
                columns: table => new
                {
                    StatementLessonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementLessons", x => x.StatementLessonID);
                    table.ForeignKey(
                        name: "FK_StatementLessons_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatementLessons_TypeOfExercise_TypeOfExerciseID",
                        column: x => x.TypeOfExerciseID,
                        principalTable: "TypeOfExercise",
                        principalColumn: "TypeOfExerciseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlaceOfBirth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentID);
                    table.ForeignKey(
                        name: "FK_Students_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarkArhives",
                columns: table => new
                {
                    MarkArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureOfTeacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    LessonID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false),
                    ThemeID = table.Column<int>(type: "int", nullable: false),
                    StudentArhiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkArhives", x => x.MarkArhiveID);
                    table.ForeignKey(
                        name: "FK_MarkArhives_StudentArhives_StudentArhiveID",
                        column: x => x.StudentArhiveID,
                        principalTable: "StudentArhives",
                        principalColumn: "StudentArhiveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarkArhives_Themes_ThemeID",
                        column: x => x.ThemeID,
                        principalTable: "Themes",
                        principalColumn: "ThemeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatementMarkArhives",
                columns: table => new
                {
                    StatementMarkArhiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureOfTeacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    StatementLessonID = table.Column<int>(type: "int", nullable: false),
                    StudentArhiveID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementMarkArhives", x => x.StatementMarkArhiveID);
                    table.ForeignKey(
                        name: "FK_StatementMarkArhives_StudentArhives_StudentArhiveID",
                        column: x => x.StudentArhiveID,
                        principalTable: "StudentArhives",
                        principalColumn: "StudentArhiveID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marks",
                columns: table => new
                {
                    MarkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureOfTeacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlagF = table.Column<int>(type: "int", nullable: false),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SubjectID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    LessonID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false),
                    ThemeID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marks", x => x.MarkID);
                    table.ForeignKey(
                        name: "FK_Marks_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Marks_Themes_ThemeID",
                        column: x => x.ThemeID,
                        principalTable: "Themes",
                        principalColumn: "ThemeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatementMarks",
                columns: table => new
                {
                    StatementMarkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureOfTeacher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstituteID = table.Column<int>(type: "int", nullable: false),
                    SpecialityID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    TypeOfExerciseID = table.Column<int>(type: "int", nullable: false),
                    StatementLessonID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementMarks", x => x.StatementMarkID);
                    table.ForeignKey(
                        name: "FK_StatementMarks_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
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

            migrationBuilder.CreateIndex(
                name: "IX_GroupArhives_SpecialityID",
                table: "GroupArhives",
                column: "SpecialityID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_DateExit",
                table: "Groups",
                column: "DateExit");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SpecialityID",
                table: "Groups",
                column: "SpecialityID");

            migrationBuilder.CreateIndex(
                name: "IX_JournalArhives_GroupArhiveID",
                table: "JournalArhives",
                column: "GroupArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_JournalArhives_SubjectID",
                table: "JournalArhives",
                column: "SubjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_GroupID_SubjectID",
                table: "Journals",
                columns: new[] { "GroupID", "SubjectID" });

            migrationBuilder.CreateIndex(
                name: "IX_Journals_SubjectID",
                table: "Journals",
                column: "SubjectID");

            migrationBuilder.CreateIndex(
                name: "IX_LessonArhives_GroupArhiveID",
                table: "LessonArhives",
                column: "GroupArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_LessonArhives_SubjectID_Date_FlagF",
                table: "LessonArhives",
                columns: new[] { "SubjectID", "Date", "FlagF" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonArhives_ThemeID",
                table: "LessonArhives",
                column: "ThemeID");

            migrationBuilder.CreateIndex(
                name: "IX_LessonArhives_TypeOfExerciseID",
                table: "LessonArhives",
                column: "TypeOfExerciseID");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_GroupID",
                table: "Lessons",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectID_Date_FlagF",
                table: "Lessons",
                columns: new[] { "SubjectID", "Date", "FlagF" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ThemeID",
                table: "Lessons",
                column: "ThemeID");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TypeOfExerciseID",
                table: "Lessons",
                column: "TypeOfExerciseID");

            migrationBuilder.CreateIndex(
                name: "IX_MarkArhives_Date_FlagF_SubjectID_GroupID",
                table: "MarkArhives",
                columns: new[] { "Date", "FlagF", "SubjectID", "GroupID" });

            migrationBuilder.CreateIndex(
                name: "IX_MarkArhives_StudentArhiveID",
                table: "MarkArhives",
                column: "StudentArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_MarkArhives_ThemeID",
                table: "MarkArhives",
                column: "ThemeID");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_Date_FlagF_SubjectID_GroupID",
                table: "Marks",
                columns: new[] { "Date", "FlagF", "SubjectID", "GroupID" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_StudentID",
                table: "Marks",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_ThemeID",
                table: "Marks",
                column: "ThemeID");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_InstituteID",
                table: "Specialities",
                column: "InstituteID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementLessonArhives_GroupArhiveID",
                table: "StatementLessonArhives",
                column: "GroupArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementLessonArhives_TypeOfExerciseID",
                table: "StatementLessonArhives",
                column: "TypeOfExerciseID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementLessons_GroupID",
                table: "StatementLessons",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementLessons_TypeOfExerciseID",
                table: "StatementLessons",
                column: "TypeOfExerciseID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementMarkArhives_StudentArhiveID",
                table: "StatementMarkArhives",
                column: "StudentArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_StatementMarks_StudentID",
                table: "StatementMarks",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentArhives_GroupArhiveID",
                table: "StudentArhives",
                column: "GroupArhiveID");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GroupID",
                table: "Students",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DepartmentID",
                table: "Subjects",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Themes_SubjectID",
                table: "Themes",
                column: "SubjectID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "El_Stud_Links");

            migrationBuilder.DropTable(
                name: "ElectiveMarks");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "JournalArhives");

            migrationBuilder.DropTable(
                name: "Journals");

            migrationBuilder.DropTable(
                name: "LessonArhives");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "MarkArhives");

            migrationBuilder.DropTable(
                name: "Marks");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "StatementLessonArhives");

            migrationBuilder.DropTable(
                name: "StatementLessons");

            migrationBuilder.DropTable(
                name: "StatementMarkArhives");

            migrationBuilder.DropTable(
                name: "StatementMarks");

            migrationBuilder.DropTable(
                name: "Sub_SpecLinks");

            migrationBuilder.DropTable(
                name: "TeacherNoPCs");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "ElectiveLessons");

            migrationBuilder.DropTable(
                name: "Themes");

            migrationBuilder.DropTable(
                name: "TypeOfExercise");

            migrationBuilder.DropTable(
                name: "StudentArhives");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "ElectiveThemes");

            migrationBuilder.DropTable(
                name: "ElectiveTypes");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "GroupArhives");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Electives");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Specialities");

            migrationBuilder.DropTable(
                name: "Institutes");
        }
    }
}
