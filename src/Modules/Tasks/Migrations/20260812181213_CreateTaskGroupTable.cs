using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasks.Migrations
{
    /// <inheritdoc />
    public partial class CreateTaskGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "taskgroups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taskgroups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_taskgroups_title",
                table: "taskgroups",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "taskgroups");
        }
    }
}
