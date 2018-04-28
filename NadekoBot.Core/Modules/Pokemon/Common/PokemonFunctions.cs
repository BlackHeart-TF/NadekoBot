using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Pokemon.Common;
using NadekoBot.Modules.Pokemon.Extentions;
using NadekoBot.Modules.Pokemon.Services;

namespace NadekoBot.Modules.Pokemon.Common
{
    static class PokemonFunctions
    {
        private static DbService _db = PokemonNew.GetDb();
        private static readonly PokemonService service = PokemonService.pokemonInstance;

        public static List<PokemonSprite> PokemonList(IUser u)
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var row = db.Where(x => x.OwnerId == (long)u.Id);
            if (row.Count() >= 6)
            {
                return row.ToList();
            }
            else
            {

                var list = new List<PokemonSprite>();
                while (row.Count() + list.Count < 6)
                {
                    var pkm = GeneratePokemon(u);
                    if (!list.Where(x => x.IsActive).Any())
                    {
                        pkm.IsActive = true;
                    }

                    list.Add(pkm);


                }
                //Set an active pokemon
                var uow = _db.UnitOfWork;
                uow.PokemonSprite.AddRange(list.ToArray());
                uow.CompleteAsync();

                return list;
            }
        }

        private static NadekoRandom rng = new NadekoRandom();
        public static  PokemonSprite GeneratePokemon(IUser u)
        {

            var list = service.pokemonClasses.Where(x => x.EvolveLevel != -1).ToList();
            var speciesIndex = rng.Next(0, list.Count() - 1);
            rng.Next();
            var species = list[speciesIndex];

            PokemonSprite sprite = new PokemonSprite
            {
                SpeciesId = species.Number,
                HP = species.BaseStats["hp"],
                Level = 1,
                NickName = species.Name,
                OwnerId = (long)u.Id,
                XP = 0,
                Attack = species.BaseStats["attack"],
                Defense = species.BaseStats["defense"],
                SpecialAttack = species.BaseStats["special-attack"],
                SpecialDefense = species.BaseStats["special-defense"],
                Speed = species.BaseStats["speed"],
                MaxHP = species.BaseStats["hp"]
            };

            while (sprite.Level < 4)
            {
                sprite.LevelUp();
            }
            sprite.XP = sprite.XPRequired();
            sprite.LevelUp();
            return sprite;
        }
        public static async void UpdatePokemon(PokemonSprite pokemon)
        {
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Update(pokemon);
            await uow.CompleteAsync();
        }

        public static async void DeletePokemon(PokemonSprite pokemon)
        {
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Remove(pokemon);
            await uow.CompleteAsync();
        }

        public static Task<List<PokemonTrainer>> GetTopPlayersAsync() =>
            Task.Run(() => { return GetTopPlayers(); });

        public static List<PokemonTrainer> GetTopPlayers()
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var users = db.DistinctBy(x => x.OwnerId);
            var output = new List<PokemonTrainer>();
            foreach (var user in users)
            {
                var usrpkm = db.Where(x => x.OwnerId == user.OwnerId);
                long exp = 0;
                foreach (var pkm in usrpkm)
                    exp += pkm.XP;
                var trainer = new PokemonTrainer()
                {
                    ID = user.OwnerId,
                    TotalExp = exp,
                    TopPokemon = usrpkm.OrderByDescending(x => x.Level).First()
                };
                output.Add(trainer);
            }
            output = output.OrderByDescending(x => x.TotalExp).Take(4).ToList();
            return output;
        }

        public static Task<List<PokemonTrainer>> GetPlayerRankAsync(IUser player) =>
            Task.Run(() => { return GetPlayerRank(player); });

        public static List<PokemonTrainer> GetPlayerRank(IUser player)
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var users = db.DistinctBy(x => x.OwnerId);
            var output = new List<PokemonTrainer>();
            //int player 
            foreach (var user in users)
            {
                var usrpkm = db.Where(x => x.OwnerId == user.OwnerId);
                long exp = 0;
                foreach (var pkm in usrpkm)
                    exp += pkm.XP;
                var trainer = new PokemonTrainer()
                {
                    ID = user.OwnerId,
                    TotalExp = exp,
                    TopPokemon = usrpkm.OrderByDescending(x => x.Level).First()
                };
                output.Add(trainer);
            }
            output = output.OrderByDescending(x => x.TotalExp).ToList();
            for (int i = 0; i < output.Count; i++)
                output[i].Rank = i + 1;
            var playerRank = output.Where(x => x.ID == (long)player.Id).First().Rank;
            output = output.Skip(playerRank - 3).Take(5).ToList();
            return output;
        }

        public static PokemonSprite GetActivePokemon(IUser user)
        {
            var list = PokemonList(user);
            var active = list.Where(x => x.IsActive).FirstOrDefault();
            if (active == null)
            {
                var pkm = list.Where(x => x.HP > 0).FirstOrDefault() ?? list.First();
                pkm.IsActive = true;
                UpdatePokemon(pkm);
                active = pkm;
            }
            return active;
        }
    }
}
