using System;
using PokeAPI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PokemonJsonBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("PokemonSpecies.json"))
            {
                Console.WriteLine("PokemonSpecies.json File Exists. Delete? Y/n");
                ConsoleKeyInfo x = Console.ReadKey();
                if (x.Key == ConsoleKey.Y)
                    File.Delete("PokemonSpecias.json");
                else return;
            }

            using (StreamWriter sw = new StreamWriter("PokemonSpecies.json"))
            {

                for (int i = 1; i <= 949; i++)
                {
                    Console.WriteLine($"Writing pokemon {i} out of 949..");
                    sw.WriteLine(JsonConvert.SerializeObject(SpeciesMeCapn(i), Formatting.Indented));
                    System.Threading.Thread.Sleep(30000);
                }
            }
            Console.WriteLine("Finished.");
            Console.ReadKey();
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
                evolution = DataFetcher.GetApiObject<EvolutionChain>(pkmNumber).Result;
                pokemon = DataFetcher.GetApiObject<Pokemon>(pkmNumber).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception  thrown {e.Message}. Retrying in 60s..");
                System.Threading.Thread.Sleep(60000);
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
                baseStats = stats,
                ImageLink = $"http://pokeapi.co/media/sprites/pokemon/{species.ID}.png"
                
            };
            //evolveto
            if (evolution.Chain.EvolvesTo.Count() > 0)
            {
                sprite.EvolveTo = DataFetcher.GetAny<PokemonSpecies>(evolution.Chain.EvolvesTo[0].Species.Url).Result.ID;
                //EvolveLevel
                if (evolution.Chain.EvolvesTo[0].Details[0].Trigger.Name == "level-up")
                    sprite.EvolveLevel = evolution.Chain.EvolvesTo[0].Details[0].MinLevel;
            }
            else sprite.EvolveTo = 0;
           
            //Types
            var TypeList = new List<string>();
            foreach (var type in pokemon.Types)
                TypeList.Add(type.Type.Name);
            sprite.Types = TypeList.ToArray();

            //done
            return sprite;
        }
    }

    public class NewPokemonSpecies
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public float CatchRate { get; set; }
        public int BaseExperience { get; set; }
        public BaseStats baseStats { get; set; }
        public int? EvolveLevel { get; set; }
        public int EvolveTo { get; set; }
        public string[] Types { get; set; }
        public PokemonMove[] LearnSet { get; set; }
        public string ImageLink { get; set; }
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

    public class PokemonMove
    {
        int ID;
        string Name;
        int LearnLevel;
    }
}
