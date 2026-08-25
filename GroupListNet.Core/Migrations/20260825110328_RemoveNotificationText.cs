using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotificationText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "text",
                table: "notifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
