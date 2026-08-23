using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class ErrorMessageNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_message",
                table: "notifications");
        }
    }
}
