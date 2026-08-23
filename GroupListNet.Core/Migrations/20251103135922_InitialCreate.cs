using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GroupListNet.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    father_name = table.Column<string>(type: "text", nullable: false),
                    surname = table.Column<string>(type: "text", nullable: false),
                    number_in_group = table.Column<int>(type: "integer", nullable: false),
                    telegram_id = table.Column<string>(type: "text", nullable: false),
                    subgroup = table.Column<int>(type: "integer", nullable: true),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leader",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leader", x => x.id);
                    table.ForeignKey(
                        name: "FK_leader_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subgroup = table.Column<int>(type: "integer", nullable: true),
                    week_type = table.Column<int>(type: "integer", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    class_type = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    notification_type = table.Column<int>(type: "integer", nullable: false),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notifications_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_ScheduleId",
                table: "attendance",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_StudentId",
                table: "attendance",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_leader_StudentId",
                table: "leader",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ScheduleId",
                table: "notifications",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_StudentId",
                table: "notifications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_SubjectId",
                table: "schedule",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "leader");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "schedule");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "subjects");
        }
    }
}
