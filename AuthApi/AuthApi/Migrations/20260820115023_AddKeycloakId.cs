using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddKeycloakId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "KeycloakId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KeycloakId",
                table: "Users",
                newName: "Password");
        }
    }
}
