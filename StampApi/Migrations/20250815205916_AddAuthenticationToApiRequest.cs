using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StampApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationToApiRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authentication",
                table: "ApiRequests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authentication",
                table: "ApiRequests");
        }
    }
}
