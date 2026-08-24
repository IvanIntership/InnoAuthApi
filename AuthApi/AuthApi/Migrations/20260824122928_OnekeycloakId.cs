using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.API.Migrations
{
    /// <inheritdoc />
    public partial class OnekeycloakId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeycloakId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeycloakId",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
