using NadekoBot.Modules.PokeBattle.Common;
using NadekoBot.Modules.PokeBattle.Services;
using System.Linq;
using NadekoBot.Db.Models;
using Discord;
using System.Threading.Tasks;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.PokeBattle.Extentions;

public static class Extentions 
{

    public static ConcurrentDictionary<ulong, TrainerStats> UserStats = new ConcurrentDictionary<ulong, TrainerStats>();
    public static readonly PokemonService service = PokemonService.Instance;


    public static async Task<PokemonSprite> GetUserPokemonAsync(this IUser user)
    {
        return await service.GetActivePokemonAsync(user);
    }

    public static void Update (this PokemonSprite pkm)
    {
        service.UpdatePokemon(pkm);
    }

    public static PokemonSpecies GetSpecies(this PokemonSprite pkm)
    {
        return service.GetSpecies(pkm);
    }

    public static async Task<string> PokemonMoves(this PokemonSprite pkm)
    {
        var moves = service.GetMoves(pkm);
        string str = "";
        foreach (var move in moves)
        {
            str += $"**{move.Name}** *{move.Type.ToTitleCase()}*\n";
        }
        return str;
    }

    public static int XPRequired(this PokemonSprite pkm)
    {
        //Using fast (http://bulbapedia.bulbagarden.net/wiki/Experience)
        return (int)Math.Floor((4 * Math.Pow(pkm.Level, 3)) / 5);
    }

    public static RewardType Reward(this PokemonSprite pkm, PokemonSprite defeated)
    {
        int reward;
        var downer = service.GetUserByID(pkm.OwnerId);
        if (downer.IsBot && pkm.Level > 10)
            reward = 1;
        else
            reward = CalcXPReward(pkm, defeated);
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
        return service.GetSpecies(pkm).LearnSet.Where(x => x.LearnLevel == pkm.Level).FirstOrDefault();
    }


    private static int CalcXPReward(PokemonSprite winner, PokemonSprite loser)
    {
        var a = 1;
        var b = service.GetSpecies(loser).BaseExperience;
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
        var evolveLevel = species.EvolveLevel ?? 0;
        if (evolveLevel > 0)
        {
            if (evolveLevel == pkm.Level)
            {
                //*GASP* IT'S GONNA EVOLVE
                //Play an animation?
                var newSpecies = service.pokemonClasses.Where(x => x.ID == int.Parse(species.EvolveTo)).DefaultIfEmpty(null).First();
                retString += $"**{pkm.NickName}** is Evolving!\n **{pkm.NickName}** evolved to **{newSpecies.Name}**\n";
                if (pkm.NickName == service.GetSpecies(pkm).Name)
                    pkm.NickName = newSpecies.Name;
                pkm.SpeciesId = newSpecies.ID;
                species = newSpecies;
                
            }
        }

        //learn a move?
        var learnableMove = pkm.GetLearnableMove();
        if (learnableMove != null)
        {
            if (pkm is null)
                Console.WriteLine("pkm is null");
            if (service.GetMoves(pkm).Count() >= 4)
            {
                retString += $"**{pkm.NickName}** wants to learn **{learnableMove.Name}** *({service.pokemonMoves[learnableMove.Name].Type})*!\n Use `.learn <move to replace>` to learn {learnableMove.Name}.";
                return retString;
            }
            for (int i = 1;i <= 4; i++)
            {
                var move = service.pokemonMoves[(string)typeof(PokemonSprite).GetProperty("Move" + i).GetValue(pkm)];
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

    public static PokemonSprite Rename(this PokemonSprite sprite, string newName)
    {
        sprite.NickName = newName;
        return sprite;
    }

    public static void Heal(this PokemonSprite sprite)
    {
        sprite.HP = sprite.MaxHP;
        sprite.StatusEffect = "none";
        service.UpdatePokemon(sprite);
    }

}