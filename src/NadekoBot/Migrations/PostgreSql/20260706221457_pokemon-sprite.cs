using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class pokemonsprite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pokemonsprite",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ownerid = table.Column<long>(type: "bigint", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: false),
                    hp = table.Column<int>(type: "integer", nullable: false),
                    xp = table.Column<long>(type: "bigint", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    speciesid = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    isshiny = table.Column<bool>(type: "boolean", nullable: false),
                    attack = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    maxhp = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    specialattack = table.Column<int>(type: "integer", nullable: false),
                    specialdefense = table.Column<int>(type: "integer", nullable: false),
                    move1 = table.Column<string>(type: "text", nullable: false),
                    move2 = table.Column<string>(type: "text", nullable: true),
                    move3 = table.Column<string>(type: "text", nullable: true),
                    move4 = table.Column<string>(type: "text", nullable: true),
                    statuseffect = table.Column<string>(type: "text", nullable: true),
                    statusturns = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pokemonsprite", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pokemonsprite");
        }
    }
}
