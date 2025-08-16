using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateOwnerMembershipsForExistingCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create owner memberships for existing collections that don't have any members
            migrationBuilder.Sql(@"
                INSERT INTO CollectionMembers (CollectionId, UserId, Role, JoinedAt)
                SELECT 
                    c.Id,
                    c.UserId,
                    1, -- Owner role
                    datetime('now') as JoinedAt
                FROM Collections c
                WHERE c.UserId IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM CollectionMembers cm 
                    WHERE cm.CollectionId = c.Id AND cm.UserId = c.UserId
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
