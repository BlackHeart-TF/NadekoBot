using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class PokemonSprite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PokemonSprite",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    OwnerId = table.Column<long>(nullable: false),
                    NickName = table.Column<string>(nullable: true),
                    HP = table.Column<int>(nullable: false),
                    XP = table.Column<long>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    SpeciesId = table.Column<int>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    IsShiny = table.Column<bool>(nullable: false),
                    Attack = table.Column<int>(nullable: false),
                    Defense = table.Column<int>(nullable: false),
                    MaxHP = table.Column<int>(nullable: false),
                    Speed = table.Column<int>(nullable: false),
                    SpecialAttack = table.Column<int>(nullable: false),
                    SpecialDefense = table.Column<int>(nullable: false),
                    Move1 = table.Column<string>(nullable: true),
                    Move2 = table.Column<string>(nullable: true),
                    Move3 = table.Column<string>(nullable: true),
                    Move4 = table.Column<string>(nullable: true),
                    StatusEffect = table.Column<string>(nullable: true),
                    StatusTurns = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonSprite", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokemonSprite");
        }
    }
}
