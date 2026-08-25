using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddVkIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "vk_id",
                table: "students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "messenger",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vk_id",
                table: "students");

            migrationBuilder.DropColumn(
                name: "messenger",
                table: "notifications");
        }
    }
}
