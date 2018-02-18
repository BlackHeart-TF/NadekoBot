using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace NadekoBot.Migrations
{
    public partial class PokeSprite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PokeSprite",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Attack = table.Column<int>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Defense = table.Column<int>(nullable: false),
                    HP = table.Column<int>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    MaxHP = table.Column<int>(nullable: false),
                    NickName = table.Column<string>(nullable: true),
                    OwnerId = table.Column<long>(nullable: false),
                    SpecialAttack = table.Column<int>(nullable: false),
                    SpecialDefense = table.Column<int>(nullable: false),
                    SpeciesId = table.Column<int>(nullable: false),
                    Speed = table.Column<int>(nullable: false),
                    XP = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokeSprite", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokeSprite_Id",
                table: "PokeSprite",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokeSprite");
        }
    }
}
