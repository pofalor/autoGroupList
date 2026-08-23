using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class add_room_building_schedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "building",
                table: "schedule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "room",
                table: "schedule",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "building",
                table: "schedule");

            migrationBuilder.DropColumn(
                name: "room",
                table: "schedule");
        }
    }
}
