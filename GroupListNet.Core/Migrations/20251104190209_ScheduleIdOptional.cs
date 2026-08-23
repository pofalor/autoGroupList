using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_schedule_ScheduleId",
                table: "notifications");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "notifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_schedule_ScheduleId",
                table: "notifications",
                column: "ScheduleId",
                principalTable: "schedule",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_schedule_ScheduleId",
                table: "notifications");

            migrationBuilder.AlterColumn<int>(
                name: "ScheduleId",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_schedule_ScheduleId",
                table: "notifications",
                column: "ScheduleId",
                principalTable: "schedule",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
