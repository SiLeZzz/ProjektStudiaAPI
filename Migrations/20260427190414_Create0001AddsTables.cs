using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class Create0001AddsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FIRMY",
                columns: table => new
                {
                    id_firmy = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nazwa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nip = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FIRMY", x => x.id_firmy);
                });

            migrationBuilder.CreateTable(
                name: "STANOWISKA",
                columns: table => new
                {
                    id_stanowiska = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nazwa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STANOWISKA", x => x.id_stanowiska);
                });

            migrationBuilder.CreateTable(
                name: "DZIALY",
                columns: table => new
                {
                    id_dzialu = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nazwa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    id_firmy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DZIALY", x => x.id_dzialu);
                    table.ForeignKey(
                        name: "FK_DZIALY_FIRMY_id_firmy",
                        column: x => x.id_firmy,
                        principalTable: "FIRMY",
                        principalColumn: "id_firmy",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRACOWNICY",
                columns: table => new
                {
                    id_pracownika = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    imie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nazwisko = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    id_dzialu = table.Column<long>(type: "bigint", nullable: false),
                    id_stanowiska = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRACOWNICY", x => x.id_pracownika);
                    table.ForeignKey(
                        name: "FK_PRACOWNICY_DZIALY_id_dzialu",
                        column: x => x.id_dzialu,
                        principalTable: "DZIALY",
                        principalColumn: "id_dzialu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PRACOWNICY_STANOWISKA_id_stanowiska",
                        column: x => x.id_stanowiska,
                        principalTable: "STANOWISKA",
                        principalColumn: "id_stanowiska",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UPRAWNIENIA",
                columns: table => new
                {
                    sygnatura = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nazwa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    opis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    id_pracownika_zarzadzajacego = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPRAWNIENIA", x => x.sygnatura);
                    table.ForeignKey(
                        name: "FK_UPRAWNIENIA_PRACOWNICY_id_pracownika_zarzadzajacego",
                        column: x => x.id_pracownika_zarzadzajacego,
                        principalTable: "PRACOWNICY",
                        principalColumn: "id_pracownika",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "POSIADA",
                columns: table => new
                {
                    id_pracownika = table.Column<long>(type: "bigint", nullable: false),
                    sygnatura = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_nadania = table.Column<DateOnly>(type: "date", nullable: false),
                    ważne_do = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSIADA", x => new { x.id_pracownika, x.sygnatura });
                    table.ForeignKey(
                        name: "FK_POSIADA_PRACOWNICY_id_pracownika",
                        column: x => x.id_pracownika,
                        principalTable: "PRACOWNICY",
                        principalColumn: "id_pracownika",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_POSIADA_UPRAWNIENIA_sygnatura",
                        column: x => x.sygnatura,
                        principalTable: "UPRAWNIENIA",
                        principalColumn: "sygnatura",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DZIALY_id_firmy",
                table: "DZIALY",
                column: "id_firmy");

            migrationBuilder.CreateIndex(
                name: "IX_FIRMY_nip",
                table: "FIRMY",
                column: "nip",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_POSIADA_sygnatura",
                table: "POSIADA",
                column: "sygnatura");

            migrationBuilder.CreateIndex(
                name: "IX_PRACOWNICY_id_dzialu",
                table: "PRACOWNICY",
                column: "id_dzialu");

            migrationBuilder.CreateIndex(
                name: "IX_PRACOWNICY_id_stanowiska",
                table: "PRACOWNICY",
                column: "id_stanowiska");

            migrationBuilder.CreateIndex(
                name: "IX_UPRAWNIENIA_id_pracownika_zarzadzajacego",
                table: "UPRAWNIENIA",
                column: "id_pracownika_zarzadzajacego");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POSIADA");

            migrationBuilder.DropTable(
                name: "UPRAWNIENIA");

            migrationBuilder.DropTable(
                name: "PRACOWNICY");

            migrationBuilder.DropTable(
                name: "DZIALY");

            migrationBuilder.DropTable(
                name: "STANOWISKA");

            migrationBuilder.DropTable(
                name: "FIRMY");
        }
    }
}
