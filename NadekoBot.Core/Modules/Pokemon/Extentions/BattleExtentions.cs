using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Pokemon.Common;
using NadekoBot.Modules.Pokemon;

namespace NadekoBot.Modules.Pokemon.Extentions
{
    static class BattleExtentions
    {
       
        //public static PokemonSprite GetActivePokemon(this SocketUser u)
        //{

        //    ActivePokemon();
        //    return 0;
        //}

        public static PokemonSprite Rename(this PokemonSprite sprite, string newName)
        {
            sprite.NickName = newName;
            return sprite;
        }

        public static PokemonSprite Attack(this PokemonSprite Sprite,PokemonSprite Target, string Move)
        {
            PokemonFunctions.DoAttack(Sprite, Target, Move);
            return new PokemonSprite();
        }

        public static void Heal(this PokemonSprite sprite)
        {
            sprite.HP = sprite.MaxHP;
            sprite.Update();
        }
    }
}
