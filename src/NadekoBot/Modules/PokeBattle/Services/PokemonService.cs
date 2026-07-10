using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Modules.PokeBattle.Common;
using NadekoBot.Modules.PokeBattle.Extentions;
using NadekoBot.Db.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NadekoBot.Services.Impl;
using NadekoBot.Common;
using Discord.WebSocket;
using Discord;
using LinqToDB.EntityFrameworkCore;

namespace NadekoBot.Modules.PokeBattle.Services
{
    public class PokemonService : INService
    { 
        public readonly SpeciesList pokemonClasses = new SpeciesList();
        public readonly List<PokemonType> pokemonTypes = new List<PokemonType>();
        public readonly MoveList pokemonMoves = new MoveList();

        public readonly Dictionary<string, Color> TypeColors = new Dictionary<string, Color>();

        //public const string PokemonClassesFile = "data/pokemon/pokemonBattlelist.json";
        public const string PokemonTypesFile = "data/pokemon/pokemon_types.json";
        public const string PokemonMovesFile = "data/pokemon/PokemonMoves.json";
        public const string PokemonSpeciesFile = "data/pokemon/PokemonSpecies.json";

        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly IConfigService _bc;
        private readonly CommandHandler _cmd;
        private readonly IImageCache _images;
        private readonly NadekoRandom _rng;
        private readonly ICurrencyService _cs;
        public readonly string TypingArticlesPath = "data/typing_articles3.json";
        private readonly CommandHandler _cmdHandler;

        internal static PokemonService Instance { get; private set; }

        public PokemonService(DiscordSocketClient client, DbService db, CommandHandler cmd, BotConfigService bc,
            IImageCache images, CommandHandler cmdHandler,
            ICurrencyService cs)
        {
            _client = client;
            _db = db;
            _bc = bc;
            _cmd = cmd;
            _images = images;
            _cmdHandler = cmdHandler;
            _rng = new NadekoRandom();
            _cs = cs;
            Instance = this;

            LoadColors();
            if (File.Exists(PokemonTypesFile))
            {
                pokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                pokemonTypes = new List<PokemonType>();
                //_log.Warn(PokemonTypesFile + " is missing. Pokemon types not loaded.");
            }
            if (File.Exists(PokemonMovesFile))
            {
                pokemonMoves = JsonConvert.DeserializeObject<MoveList>(File.ReadAllText(PokemonMovesFile));
            }
            else
            {
                pokemonMoves = new MoveList();
                //_log.Warn(PokemonMovesFile + " is missing. Pokemon types not loaded.");
            }
            if (File.Exists(PokemonSpeciesFile))
            {
                pokemonClasses = JsonConvert.DeserializeObject<SpeciesList>(File.ReadAllText(PokemonSpeciesFile));
            }
            else
            {
                pokemonClasses = new SpeciesList();
                //_log.Warn(PokemonSpeciesFile + " is missing. Pokemon types not loaded.");
            }
        }

        public IUser GetUserByID(long UserID)
        {
            return _client.GetUser((ulong)UserID);
        }

        public PokemonSpecies? GetSpecies(PokemonSprite sprite)
            => pokemonClasses.FirstOrDefault(x => x.ID == sprite.SpeciesId);
        
        public string? GetSpriteUrl(PokemonSprite sprite)
        {
            var species = GetSpecies(sprite);
            if (species?.Sprites is null)
                return null;

            return sprite.IsShiny ? species.Sprites.FrontShiny : species.Sprites.Front;
        }

        public async Task<IReadOnlyList<PokemonTrainer>> GetLeaderboardAsync(
            ulong? anchorUserId = null,
            int count = 4,
            int windowBefore = 2)
        {
            await using var ctx = _db.GetDbContext();

            var totals = await ctx.PokemonSprite
                                  .GroupBy(x => x.OwnerId)
                                  .Select(g => new
                                  {
                                      OwnerId = g.Key,
                                      TotalExp = g.Sum(x => x.XP),
                                  })
                                  .OrderByDescending(x => x.TotalExp)
                                  .ToListAsync();

            if (totals.Count == 0)
                return [];

            var ownerIds = totals.Select(x => x.OwnerId).ToList();
            var party = await ctx.PokemonSprite
                                 .Where(x => ownerIds.Contains(x.OwnerId))
                                 .ToListAsync();

            var partyByOwner = party.GroupBy(x => x.OwnerId)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            var ranked = new List<PokemonTrainer>();
            foreach (var entry in totals)
            {
                var user = _client.GetUser((ulong)entry.OwnerId);
                if (user?.IsBot == true)
                    continue;

                if (!partyByOwner.TryGetValue(entry.OwnerId, out var mons))
                    continue;

                var ace = mons.OrderByDescending(x => x.Level)
                              .ThenByDescending(x => x.XP)
                              .ThenByDescending(x => x.Id)
                              .FirstOrDefault();

                if (ace is null)
                    continue;

                ranked.Add(new PokemonTrainer
                {
                    Rank = ranked.Count + 1,
                    ID = entry.OwnerId,
                    TotalExp = entry.TotalExp,
                    TopPokemon = ace,
                });
            }

            if (ranked.Count == 0)
                return [];

            if (anchorUserId is null)
                return ranked.Take(count).ToList();

            var anchorIndex = ranked.FindIndex(x => x.ID == (long)anchorUserId);
            if (anchorIndex < 0)
                return ranked.Take(count).ToList();

            var skip = Math.Max(0, anchorIndex - windowBefore);
            return ranked.Skip(skip).Take(count).ToList();
        }

        public void LoadColors()
        {
            TypeColors.Add("normal", Color.Default);
            TypeColors.Add("fire", Color.Orange);
            TypeColors.Add("fighting", Color.Red);
            TypeColors.Add("water", Color.Blue);
            TypeColors.Add("flying", Color.LighterGrey);
            TypeColors.Add("grass", Color.Green);
            TypeColors.Add("poison", Color.Purple);
            TypeColors.Add("electric", Color.Gold);
            TypeColors.Add("ground", Color.DarkOrange);
            TypeColors.Add("psychic", Color.DarkMagenta);
            TypeColors.Add("rock", Color.DarkGrey);
            TypeColors.Add("ice", Color.Teal);
            TypeColors.Add("bug", Color.DarkGreen);
            TypeColors.Add("dragon", Color.DarkBlue);
            TypeColors.Add("ghost", Color.DarkPurple);
            TypeColors.Add("dark", Color.DarkerGrey);
            TypeColors.Add("steel", Color.LightGrey);
            TypeColors.Add("fairy", Color.Magenta);
            TypeColors.Add("unknown", Color.DarkTeal);
        }

        public async Task<List<PokemonSprite>> PokemonListAsync(IUser u)
        {
            await using var uow = _db.GetDbContext();

            var row = uow.PokemonSprite
                           .Where(x => x.OwnerId == (long)u.Id)
                           .ToList();

            // var db = _db.GetDbContext().PokemonSprite.GetAll();
            // var row = db.Where(x => x.OwnerId == (long)u.Id);

            if (row.Count() >= (u.IsBot ? 1 : 6))
            {
                return row.ToList();
            }
            else
            {

                var list = new List<PokemonSprite>();
                while (row.Count() + list.Count < (u.IsBot ? 1 : 6))
                {
                    var pkm = GeneratePokemon(u);
                    if (!list.Where(x => x.IsActive).Any())
                    {
                        pkm.IsActive = true;
                    }

                    list.Add(pkm);
                }
                //Set an active pokemon
                //var uow = _db.GetDbContext();
                uow.PokemonSprite.AddRange(list.ToArray());
                await uow.SaveChangesAsync();

                return list;
            }
        }

        public PokemonSprite GeneratePokemon(IUser u)
        {
            var list = pokemonClasses.Where(x => x.EvolveStage == 0).ToList();
            var speciesIndex = _rng.Next(0, list.Count() - 1);
            _rng.Next();
            var species = list[speciesIndex];
            PokemonSprite sprite = new PokemonSprite
            {
                SpeciesId = species.ID,
                HP = species.BaseStats["hp"],
                Level = 1,
                NickName = species.Name,
                OwnerId = (long)u.Id,
                XP = 0,
                IsShiny = _rng.Next(0, 65536) < 8,
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
            var moves = GetNewPkmMoves(sprite.SpeciesId);
            switch (moves.Count())
            {
                case 4:
                    sprite.Move4 = moves[3].Name;
                    goto case 3;
                case 3:
                    sprite.Move3 = moves[2].Name;
                    goto case 2;
                case 2:
                    sprite.Move2 = moves[1].Name;
                    goto case 1;
                case 1:
                    sprite.Move1 = moves[0].Name;
                    break;
            }
            return sprite;
        }

        public MoveList GetNewPkmMoves(int id)
        {
            var pkm = pokemonClasses[id];
            var learnmoves = pokemonClasses[id].LearnSet.Where(x => x.LearnLevel <= 5).Shuffle().Take(4).ToList();
            var moveList = new MoveList();
            foreach (var move in learnmoves)
            {
                var pkmMove = pokemonMoves.Where(x => x.ID == move.ID).First();
                moveList.AddIfNotNull(pkmMove);
            }
            return moveList;
        }
        public async void UpdatePokemon(PokemonSprite pokemon)
        {
            var uow = _db.GetDbContext();
            uow.PokemonSprite.Update(pokemon);
            await uow.SaveChangesAsync();
        }

        public PokemonMove? GetMoveAsync(PokemonSprite pokemon, string moveName)
        {
            var move = GetMoves(pokemon).FirstOrDefault(x => x.Name == moveName);
            return move;
        }

        public MoveList GetMoves(PokemonSprite pokemon)
        {
            if (pokemon?.Move1 == null)
            {
                return null;
            }
            var moves = new MoveList();
            moves.AddIfNotNull(pokemonMoves[pokemon.Move1]);
            moves.AddIfNotNull(pokemonMoves[pokemon.Move2]);
            moves.AddIfNotNull(pokemonMoves[pokemon.Move3]);
            moves.AddIfNotNull(pokemonMoves[pokemon.Move4]);
            
            return moves;
        }

        public async void DeletePokemon(PokemonSprite pokemon)
        {
            var uow = _db.GetDbContext();
            uow.PokemonSprite.Remove(pokemon);
            await uow.SaveChangesAsync();
        }

        public async Task<PokemonSprite> GetActivePokemonAsync(IUser user)
        {
            var list = await PokemonListAsync(user);
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

        /// <summary>
        /// Sets the active pokemon of the given user to the given Sprite
        /// </summary>
        /// <param name="u"></param>
        /// <param name="newActive"></param>
        /// <returns></returns>
        public async Task<SwitchResult> SwitchPokemonAsync(IUser user, PokemonSprite newActive)
        {
            var toUnset = (await PokemonListAsync(user)).Where(x => x.IsActive).FirstOrDefault();
            if (toUnset == null)
            {
                return SwitchResult.Failed;
            }
            if (newActive.HP <= 0)
            {
                return SwitchResult.TargetFainted;
            }
            toUnset.IsActive = false;
            newActive.IsActive = true;
            UpdatePokemon(toUnset);
            UpdatePokemon(newActive);

            return SwitchResult.Pass;
        }
    }
}
