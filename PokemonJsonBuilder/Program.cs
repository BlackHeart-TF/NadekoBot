using System;
using PokeAPI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;

namespace PokemonJsonBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            bool doSpecies = false;
            bool doMoves = false;
            if (File.Exists("PokemonSpecies.json"))
            {
                Console.Write("PokemonSpecies.json File Exists. Delete? Y/n");
                redo1:
                string x = Console.ReadLine();
                switch (x)
                {
                    case "y":
                        File.Delete("PokemonSpecias.json");
                        doSpecies = true;
                        break;
                    case "n":
                        doSpecies = false;
                        break;

                    default:
                        Console.Write("\ny or n: ");
                        goto redo1;
                }                    

            }
            if (File.Exists("PokemonMoves.json"))
            {
                Console.Write("PokemonMoves.json File Exists. Delete? Y/n");
                redo2:
                string x = Console.ReadLine();
                switch (x)
                {
                    case "y":
                        File.Delete("PokemonMoves.json");
                        doMoves = true;
                        break;
                    case "n":
                        doMoves = false;
                        break;

                    default:
                        Console.Write("\ny or n: ");
                        goto redo2;
                } 

            }
            if (!doSpecies)
                goto noSpecies;
            using (StreamWriter sw = new StreamWriter("PokemonSpecies.json"))
            {

                for (int i = 1; i <= 802; i++)
                {
                    Console.Write($"Fetching pokemon {i} out of 802.. ");
                    var pkminfo = JsonConvert.SerializeObject(SpeciesMeCapn(i), Formatting.Indented);
                    Console.Write("Writing.. ");
                    sw.WriteLine(pkminfo + ",");
                    Console.WriteLine("Done.");
                }
                sw.Flush();
            }
            noSpecies:
            if (!doMoves)
                goto noMoves;
            using (StreamWriter sw = new StreamWriter("PokemonMoves.json"))
            {

                for (int i = 1; i <= 719; i++)
                {
                    Console.Write($"Fetching move {i} out of 719.. ");
                    var pkminfo = JsonConvert.SerializeObject(MovePls(i), Formatting.Indented);
                    Console.Write("Writing.. ");
                    sw.WriteLine(pkminfo + ",");
                    Console.WriteLine("Done.");
                }
                sw.Flush();
            }
            noMoves:
            Console.WriteLine("Finished.");
            Console.ReadKey();
        }

        private static PokemonMoves MovePls(int moveID)
        {
            Move Moves;
            retry:
            try
            {
                Moves = DataFetcher.GetApiObject<Move>(moveID).Result;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception thrown {e.InnerException.Message}. Retrying in 60s..");
                System.Threading.Thread.Sleep(10000);
                goto retry;
            }
            var move = new PokemonMoves()
            {
                ID = Moves.ID,
                Name = Moves.Name,
                PP = Moves.PP,
                Type = Moves.Type.Name,
                Accuracy = Convert.ToInt32(Moves.Accuracy),
                Power = Moves.Power ?? 0,
                DamageType = Moves.DamageClass.Name,
                FlavorText = Moves.FlavorTextEntries.Where(x => x.Language.Name == "en" && x.VersionGroup.Name == "sun-moon").First().FlavorText,
                MoveEffects = new PokemonMoveEffects()
                {
                    Healing = Moves.Meta.Value.Healing,
                    Drain = Moves.Meta.Value.DrainRecoil,
                    Ailment = Moves.Meta.Value.Ailment.Name,
                    AilmentChance = Convert.ToInt32(Moves.Meta.Value.AilmentChance),
                    FlinchChance = Convert.ToInt32(Moves.Meta.Value.FlinchChance),
                    StatChance = Moves.Meta.Value.StatChance,
                    HitsMin = Moves.Meta.Value.MinHits,
                    HitsMax = Moves.Meta.Value.MaxHits,
                    MaxTurns = Moves.Meta.Value.MaxTurns,
                }

            };
            var changes = new List<StatChanges>();
            foreach(var statchange in Moves.StatChanges)
            {
                changes.Add(new StatChanges() { Stat = statchange.Stat.Name, Change = statchange.Change });
            }
            move.MoveEffects.StatChanges = changes.ToArray();
            return move;
        }

        static NewPokemonSpecies SpeciesMeCapn(int pkmNumber)
        {
            PokemonSpecies species;
            EvolutionChain evolution;
            Pokemon pokemon;
            retry:
            try
            {
                species = DataFetcher.GetApiObject<PokemonSpecies>(pkmNumber).Result;
                evolution = DataFetcher.GetAny<EvolutionChain>(species.EvolutionChain.Url).Result;
                pokemon = DataFetcher.GetAny<Pokemon>(species.Varieties[0].Pokemon.Url).Result;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception thrown {e.InnerException.Message}. Retrying in 60s..");
                System.Threading.Thread.Sleep(10000);
                goto retry;
            }

            var stats = new BaseStats(pokemon.Stats.Where(x => x.Stat.Name == "speed").First().BaseValue,
                pokemon.Stats.Where(x => x.Stat.Name == "special-defense").First().BaseValue,
                pokemon.Stats.Where(x => x.Stat.Name == "special-attack").First().BaseValue,
                pokemon.Stats.Where(x => x.Stat.Name == "defense").First().BaseValue,
                pokemon.Stats.Where(x => x.Stat.Name == "attack").First().BaseValue,
                pokemon.Stats.Where(x => x.Stat.Name == "hp").First().BaseValue);

            var sprite = new NewPokemonSpecies()
            {
                ID = species.ID,
                Name = species.Name,
                CatchRate = species.CaptureRate,
                BaseExperience = pokemon.BaseExperience,
                BaseStats = stats
            };

            sprite.Sprites = new Sprites()
            {
                Front = pokemon.Sprites.FrontMale,
                Back = pokemon.Sprites.BackMale,
                FrontShiny = pokemon.Sprites.FrontShinyMale,
                BackShiny = pokemon.Sprites.BackShinyMale
            };
            //evolveto
            //if (new[] { 210, 222, 225, 226, 227, 231, 238, 251 }.Contains(pkmNumber))
            //    sprite.EvolveTo = 0;
            if (evolution.Chain.EvolvesTo.Count() > 0)
            {
                if (DataFetcher.GetAny<PokemonSpecies>(evolution.Chain.Species.Url).Result.ID == pkmNumber && evolution.Chain.EvolvesTo.Count() > 0)
                {
                    sprite.EvolveTo = DataFetcher.GetAny<PokemonSpecies>(evolution.Chain.EvolvesTo[0].Species.Url).Result.ID;
                    if (evolution.Chain.EvolvesTo[0].Details[0].Trigger.Name == "level-up")
                        sprite.EvolveLevel = evolution.Chain.EvolvesTo[0].Details[0].MinLevel;
                }
                else if (DataFetcher.GetAny<PokemonSpecies>(evolution.Chain.EvolvesTo[0].Species.Url).Result.ID == pkmNumber && evolution.Chain.EvolvesTo[0].EvolvesTo.Count() > 0)
                {
                    sprite.EvolveTo = DataFetcher.GetAny<PokemonSpecies>(evolution.Chain.EvolvesTo[0].EvolvesTo[0].Species.Url).Result.ID;
                    if (evolution.Chain.EvolvesTo[0].EvolvesTo[0].Details[0].Trigger.Name == "level-up")
                        sprite.EvolveLevel = evolution.Chain.EvolvesTo[0].EvolvesTo[0].Details[0].MinLevel;
                }
                else
                {
                    sprite.EvolveLevel = 0;
                }

            }
            else sprite.EvolveTo = 0;
            if (evolution.Chain.Species.Name == sprite.Name || new[] { 489 }.Contains(sprite.ID))
                sprite.EvolveLevel = 0;
            else if (evolution.Chain.EvolvesTo.Count() > 0)
            {
                if (evolution.Chain.EvolvesTo[0].Species.Name == sprite.Name)
                    sprite.EvolveLevel = 1;
            }
            else if (evolution.Chain.EvolvesTo[0].EvolvesTo.Count() > 0)
            {
                if (evolution.Chain.EvolvesTo[0].EvolvesTo[0].Species.Name == species.Name)
                    sprite.EvolveLevel = 2;
            }
            else
                sprite.EvolveLevel = 3;
            //moves
            sprite.LearnSet = GetLearnSet(pokemon.Moves);

            //Types
            var TypeList = new List<string>();
            foreach (var type in pokemon.Types)
                TypeList.Add(type.Type.Name);
            sprite.Types = TypeList.ToArray();
            //done
            return sprite;
        }
        private static PokemonLearnMoves[] GetLearnSet(PokemonMove[] moves)
        {
            var moveList = new List<PokemonLearnMoves>();
            foreach (var move in moves)
            {
                try
                {
                    var llg = move.VersionGroupDetails.Where(y => y.VersionGroup.Name == "sun-moon" && y.LearnMethod.Name == "level-up").First();
                    var moveData = DataFetcher.GetAny<Move>(move.Move.Url).Result;
                    var Move = new PokemonLearnMoves(moveData.ID, move.Move.Name, llg.LearnedAt);
                    moveList.Add(Move);
                }
                catch (InvalidOperationException) { }
            }
            

            return moveList.ToArray();
        }
    }

    
    public class NewPokemonSpecies
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public float CatchRate { get; set; }
        public int BaseExperience { get; set; }
        public BaseStats BaseStats { get; set; }
        public int EvolveStage { get; set; }
        public int? EvolveLevel { get; set; }
        public int EvolveTo { get; set; }
        public string[] Types { get; set; }
        public PokemonLearnMoves[] LearnSet { get; set; }
        public Sprites Sprites { get; set; }
    }
    public class BaseStats
    {
        [JsonProperty("speed")]
        int Speed { get; set; }

        [JsonProperty("special-defense")]
        int SpecialDefense { get; set; }

        [JsonProperty("special-attack")]
        int SpecialAttack { get; set; }

        [JsonProperty("defense")]
        int Defense { get; set; }

        [JsonProperty("attack")]
        int Attack { get; set; }

        [JsonProperty("hp")]
        int HP { get; set; }

        public BaseStats(int speed, int specialdefence, int specialattack, int defence, int attack, int hp)
        {
            Speed = speed;
            SpecialDefense = specialdefence;
            SpecialAttack = specialattack;
            Defense = defence;
            Attack = attack;
            HP = hp;
        }
    }
    public class Sprites
    {
        public string Front;
        public string Back;
        public string FrontShiny;
        public string BackShiny;
    }
    public class PokemonLearnMoves
    {
        [JsonProperty("ID")]
        int ID;
        [JsonProperty("Name")]
        string Name;
        [JsonProperty("LearnLevel")]
        int LearnLevel;

        public PokemonLearnMoves(int id, string name, int learnlevel)
        {
            ID = id;
            Name = name;
            LearnLevel = learnlevel;
        }
    }
    public class PokemonMoves
    {
        public int ID;
        public string Name;
        public int? PP;
        public string Type;
        public int Accuracy;
        public int Power;
        public string DamageType;
        public string FlavorText;
        public PokemonMoveEffects MoveEffects;
    }
    public class PokemonMoveEffects
    {
        public int Healing;
        public int Drain;
        public string Ailment;
        public int AilmentChance;
        public int FlinchChance;
        public int StatChance;
        public int? HitsMin;
        public int? HitsMax;
        public int? MaxTurns;
        public StatChanges[] StatChanges;
    }
    public class StatChanges
    {
        public string Stat;
        public int Change;
    }
}
