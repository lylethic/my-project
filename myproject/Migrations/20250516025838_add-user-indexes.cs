using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myproject.Migrations
{
    /// <inheritdoc />
    public partial class adduserindexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "user",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Name",
                table: "user",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Email",
                table: "user");

            migrationBuilder.DropIndex(
                name: "IX_User_Name",
                table: "user");
        }
    }
}
