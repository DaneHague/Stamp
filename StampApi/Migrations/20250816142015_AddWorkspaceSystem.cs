using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "Collections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_WorkspaceId",
                table: "Collections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_UserId",
                table: "Workspaces",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Workspaces_WorkspaceId",
                table: "Collections",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create default workspaces for existing users with collections
            migrationBuilder.Sql(@"
                INSERT INTO Workspaces (Name, Description, CreatedAt, UpdatedAt, UserId)
                SELECT 'My Workspace', 'Default workspace', datetime('now'), datetime('now'), UserId
                FROM Collections
                WHERE UserId IS NOT NULL
                GROUP BY UserId;
            ");

            // Assign collections to their user's default workspace
            migrationBuilder.Sql(@"
                UPDATE Collections 
                SET WorkspaceId = (
                    SELECT Id FROM Workspaces 
                    WHERE Workspaces.UserId = Collections.UserId 
                    AND Workspaces.Name = 'My Workspace'
                    LIMIT 1
                )
                WHERE UserId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Workspaces_WorkspaceId",
                table: "Collections");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Collections_WorkspaceId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Collections");
        }
    }
}
