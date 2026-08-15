using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasks.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserForeignKeyFromTaskGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups
                DROP CONSTRAINT IF EXISTS fk_taskgroups_users_user_id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups
                ADD CONSTRAINT fk_taskgroups_users_user_id
                FOREIGN KEY (user_id)
                REFERENCES users(id)
                ON DELETE CASCADE;
            ");
        }
    }
}
