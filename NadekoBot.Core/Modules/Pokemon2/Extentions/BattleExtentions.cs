using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Pokemon2;

namespace NadekoBot.Modules.Pokemon2.Extentions
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

        public static PokemonSprite Attack(this PokemonSprite sprite,PokemonSprite target)
        {
            throw (new NotImplementedException());
            return sprite;
        }

        public static PokemonSprite Heal(this PokemonSprite sprite)
        {
            sprite.HP = sprite.MaxHP;
            return sprite;
        }
    }
}
