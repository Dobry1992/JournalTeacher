using Microsoft.EntityFrameworkCore.Migrations;

namespace Portal.Migrations
{
    public partial class AddChangeCounter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangeCounter",
                table: "Marks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeCounter",
                table: "Marks");
        }
    }
}
