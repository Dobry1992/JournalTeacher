using Microsoft.EntityFrameworkCore.Migrations;

namespace Portal.Migrations
{
    public partial class AddShortNameToType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "TypeOfExercise",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "TypeOfExercise");
        }
    }
}
