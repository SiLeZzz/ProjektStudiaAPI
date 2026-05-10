using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "czy_aktywny",
                table: "PRACOWNICY",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "hash_hasla",
                table: "PRACOWNICY",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "login",
                table: "PRACOWNICY",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rola",
                table: "PRACOWNICY",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PRACOWNICY_login",
                table: "PRACOWNICY",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PRACOWNICY_login",
                table: "PRACOWNICY");

            migrationBuilder.DropColumn(
                name: "czy_aktywny",
                table: "PRACOWNICY");

            migrationBuilder.DropColumn(
                name: "hash_hasla",
                table: "PRACOWNICY");

            migrationBuilder.DropColumn(
                name: "login",
                table: "PRACOWNICY");

            migrationBuilder.DropColumn(
                name: "rola",
                table: "PRACOWNICY");
        }
    }
}
