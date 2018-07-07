using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NadekoBot.Migrations
{
    public partial class Pokemonmoves : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Move1",
                table: "PokeSprite",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Move2",
                table: "PokeSprite",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Move3",
                table: "PokeSprite",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Move4",
                table: "PokeSprite",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Move1",
                table: "PokeSprite");

            migrationBuilder.DropColumn(
                name: "Move2",
                table: "PokeSprite");

            migrationBuilder.DropColumn(
                name: "Move3",
                table: "PokeSprite");

            migrationBuilder.DropColumn(
                name: "Move4",
                table: "PokeSprite");
        }
    }
}
