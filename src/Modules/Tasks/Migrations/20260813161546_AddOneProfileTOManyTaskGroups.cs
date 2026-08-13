using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasks.Migrations
{
    /// <inheritdoc />
    public partial class AddOneProfileTOManyTaskGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "profile_id",
                table: "taskgroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_taskgroups_profile_id",
                table: "taskgroups",
                column: "profile_id");

            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                ADD CONSTRAINT fk_taskgroups_profiles_profile_id 
                FOREIGN KEY (profile_id) 
                REFERENCES profiles(id) 
                ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {   
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                DROP CONSTRAINT fk_taskgroups_profiles_profile_id;
            ");

            migrationBuilder.DropIndex(
                name: "ix_taskgroups_profile_id",
                table: "taskgroups");

            migrationBuilder.DropColumn(
                name: "profile_id",
                table: "taskgroups");
        }
    }
}
