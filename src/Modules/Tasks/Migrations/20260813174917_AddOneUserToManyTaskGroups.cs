using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasks.Migrations
{
    /// <inheritdoc />
    public partial class AddOneUserToManyTaskGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {   
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                DROP CONSTRAINT IF EXISTS fk_taskgroups_profiles_profile_id;
            ");

            migrationBuilder.RenameColumn(
                name: "profile_id",
                table: "taskgroups",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_taskgroups_profile_id",
                table: "taskgroups",
                newName: "ix_taskgroups_user_id");

            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                ADD CONSTRAINT fk_taskgroups_users_user_id 
                FOREIGN KEY (user_id) 
                REFERENCES users(id) 
                ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {   
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                DROP CONSTRAINT IF EXISTS fk_taskgroups_users_user_id;
            ");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "taskgroups",
                newName: "profile_id");

            migrationBuilder.RenameIndex(
                name: "ix_taskgroups_user_id",
                table: "taskgroups",
                newName: "ix_taskgroups_profile_id");
            
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                ADD CONSTRAINT fk_taskgroups_profiles_profile_id 
                FOREIGN KEY (profile_id) 
                REFERENCES profiles(id) 
                ON DELETE CASCADE;
            ");
        }
    }
}
