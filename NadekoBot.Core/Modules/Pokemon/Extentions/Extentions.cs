using NadekoBot.Modules.Pokemon.Common;
using System;
using System.Collections.Generic;
using System.Text;
using NadekoBot.Modules.Pokemon.Services;
using System.Linq;
using NadekoBot.Core.Services.Database.Models;
using Discord;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Pokemon.Extentions
{
    static class Extentions 
    {

        public static ConcurrentDictionary<ulong, TrainerStats> UserStats = new ConcurrentDictionary<ulong, TrainerStats>();
        public static readonly PokemonService service = PokemonService.pokemonInstance;


        public static PokemonSpecies GetSpecies(this PokemonSprite pkm)
        {
            return  service.pokemonClasses.Where(x => x.ID == pkm.SpeciesId).DefaultIfEmpty(null).First();
        }

        public static PokemonSprite ActivePokemon(this IUser user)
        {
            return PokemonFunctions.GetActivePokemon(user);
        }

        public static IUser GetOwner(this PokemonSprite pkm)
        {
            return service.GetUserByID(pkm.OwnerId);
        }

        public static List<PokemonSprite> GetPokemon(this IUser user)
        {
            return PokemonFunctions.PokemonList(user);
        }


        public static void Update (this PokemonSprite pkm)
        {
            PokemonFunctions.UpdatePokemon(pkm);
        }

        public static void Delete(this PokemonSprite pkm)
        {
            PokemonFunctions.DeletePokemon(pkm);
        }
        
        public static async Task<string> PokemonMoves(this PokemonSprite pkm)
        {
            var moves = await pkm.GetMoves();
            string str = "";
            foreach (var move in moves)
            {
                str += $"**{move.Name}** *{move.Type}*\n";
            }
            return str;
        }

        public static async Task<MoveList> GetMoves(this PokemonSprite pkm)
        {
            return await PokemonFunctions.GetMovesAsync(pkm);
        }
        public static int XPRequired(this PokemonSprite pkm)
        {
            //Using fast (http://bulbapedia.bulbagarden.net/wiki/Experience)
            return (int)Math.Floor((4 * Math.Pow(pkm.Level, 3)) / 5);
        }

        public static RewardType Reward(this PokemonSprite pkm, PokemonSprite defeated)
        {
            var reward = CalcXPReward(pkm, defeated);
            return pkm.GiveReward(reward);
            
        }

        public static RewardType GiveReward(this PokemonSprite pkm, int reward)
        {
            var retReward = new RewardType
            {
                RewardValue = reward.ToString()
            };
            pkm.XP += reward;
            if (pkm.XP > pkm.XPRequired())
            {
                retReward.EvolutionText = pkm.LevelUp();
            }
            return retReward;
        }

        public static PokemonLearnMoves GetLearnableMove(this PokemonSprite pkm)
        {
            return pkm.GetSpecies().LearnSet.Where(x => x.LearnLevel == pkm.Level).FirstOrDefault();
        }


        private static int CalcXPReward(PokemonSprite winner, PokemonSprite loser)
        {
            var a = 1;
            var b = loser.GetSpecies().BaseExperience;
            var L = loser.Level;
            var s = 1;
            var L_p = winner.Level;
            var t = 1;
            //Give them all a lucky egg
            var e = 1.5;
            var p = 1;
            var result = (((a * b * L) / (5 * s)) * (Math.Pow(2 * L + 10, 2.5) / Math.Pow(L + L_p + 10, 2.5)) + 1) * t * e * p;
            return (int)Math.Ceiling(result);
        }

        public static List<PokemonType> GetPokemonTypes(this PokemonSpecies spe)
        {
            var list = new List<PokemonType>();
            foreach (var typeString in spe.Types)
            {
                var t = typeString.ToUpperInvariant();
                list.Add(service.pokemonTypes.Where(x => x.Name == t).FirstOrDefault());
            }
            return list;
        }

        public static PokemonType StringToPokemonType(this string s)
        {
            var str = s.ToUpperInvariant();
            return service.pokemonTypes.Where(x => x.Name == str).DefaultIfEmpty(null).FirstOrDefault();

        }
        public static TrainerStats GetTrainerStats(this IUser user)
        {
           var stats = UserStats.GetOrAdd(user.Id, new TrainerStats(user));
            return stats;
        }
        public static void UpdateTrainerStats(this IUser user, TrainerStats stats)
        {
            UserStats.AddOrUpdate(user.Id, x => stats, (s, t) => stats);
        }
        /// <summary>
        /// levels up the pokemon, along with all the accompanying changes; including evolution
        /// </summary>
        /// <param name="pkm"></param>
        /// <returns></returns>
        public static string LevelUp(this PokemonSprite pkm)
        {
            string retString = "";
            Random rng = new Random();
            var species = pkm.GetSpecies();
            var baseStats = species.BaseStats;
            pkm.Level += 1;
            var oldhp = pkm.MaxHP;
            //Up them stats
            pkm.MaxHP = (int)Math.Ceiling((((baseStats["hp"] + rng.Next(0, 12)) + (Math.Sqrt((655535 / 100) * pkm.Level) / 4) * pkm.Level) / 100 + pkm.Level + 10));
            pkm.Attack = CalcStat(baseStats["attack"], pkm.Level);
            pkm.Defense = CalcStat(baseStats["defense"], pkm.Level);
            pkm.SpecialAttack = CalcStat(baseStats["special-attack"], pkm.Level);
            pkm.SpecialDefense = CalcStat(baseStats["special-defense"], pkm.Level);
            pkm.HP += pkm.MaxHP-oldhp;
            pkm.Speed = CalcStat(baseStats["speed"], pkm.Level);

            //Will it evolve!?
            var evolveLevel = species.EvolveLevel;
            if (evolveLevel > 0)
            {
                if (evolveLevel == pkm.Level)
                {
                    //*GASP* IT'S GONNA EVOLVE
                    //Play an animation?
                    var newSpecies = service.pokemonClasses.Where(x => x.ID == int.Parse(species.EvolveTo)).DefaultIfEmpty(null).First();
                    retString += $"**{pkm.NickName}** is Evolving!\n **{pkm.NickName}** evolved to **{newSpecies.Name}**\n";
                    if (pkm.NickName == pkm.GetSpecies().Name)
                        pkm.NickName = newSpecies.Name;
                    pkm.SpeciesId = newSpecies.ID;
                    species = newSpecies;
                    
                }
            }

            //learn a move?
            var learnableMove = pkm.GetLearnableMove();
            if (learnableMove != null)
            {
                if (pkm.GetMoves().Result.Count() >= 4)
                {
                    retString += $"**{pkm.NickName}** wants to learn **{learnableMove.Name}** *({service.pokemonMoves[learnableMove.Name].Type})*!\n Use `.learn <move to replace>` to learn {learnableMove.Name}.";
                    return retString;
                }
                for (int i = 1;i <= 4; i++)
                {
                    var move = (PokemonMove)typeof(PokemonSprite).GetProperty("Move" + i).GetValue(pkm);
                    if (move != null)
                        continue;
                    
                    typeof(PokemonSprite).GetProperty("Move" + i).SetValue(pkm, learnableMove.Name);
                    retString += $"**{pkm.NickName}** learnt the move **{learnableMove.Name}**!";
                    break;
                }
            }
            return retString;
        }

        private static int CalcStat(int _base, int level)
        {
            Random rng = new Random();
            var m = (((_base + rng.Next(0, 12)) * 2 + (Math.Sqrt((655535 / 100) * level) / 4)) * level / 100) + level + 5;
            return (int)Math.Ceiling(m);
        }


        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
    (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
