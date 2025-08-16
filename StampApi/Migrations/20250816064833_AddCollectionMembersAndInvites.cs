using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionMembersAndInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    InviteToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionInvites_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CollectionInvites_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionInvites_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionMembers_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionInvites_AcceptedByUserId",
                table: "CollectionInvites",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionInvites_CollectionId",
                table: "CollectionInvites",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionInvites_InvitedByUserId",
                table: "CollectionInvites",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionInvites_InviteToken",
                table: "CollectionInvites",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMembers_CollectionId_UserId",
                table: "CollectionMembers",
                columns: new[] { "CollectionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMembers_UserId",
                table: "CollectionMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionInvites");

            migrationBuilder.DropTable(
                name: "CollectionMembers");
        }
    }
}
