using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Pokemon.Common
{
    public class PokemonType
    {
        public PokemonType(string n, string i, string[] m, List<PokemonMultiplier> multi)
        {
            Name = n;
            Icon = i;
            Moves = m;
            Multipliers = multi;
        }
        public string Name { get; set; }
        public List<PokemonMultiplier> Multipliers { get; set; }
        public string Icon { get; set; }
        public string[] Moves { get; set; }
    }
    public class PokemonMultiplier
    {
        public PokemonMultiplier(string t, double m)
        {
            Type = t;
            Multiplication = m;
        }
        public string Type { get; set; }
        public double Multiplication { get; set; }
    }

    public class PokemonSpecies
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int BaseExperience { get; set; }
        public Dictionary<string, int> BaseStats { get; set; }
        public int EvolveStage { get; set; }
        public int EvolveLevel { get; set; }
        public string EvolveTo { get; set; }
        public string[] Types { get; set; }
        public PokemonLearnMoves[] LearnSet { get; set; }
        public Sprites Sprites { get; set; }

        public string GetTypeString()
        {
            string types = "";
            foreach (var type in this.Types)
                types += type.ToTitleCase() + "/";
            types = types.TrimEnd('/');
            return types;
        }
    }
    public class Sprites
    {
        public string Front;
        public string Back;
        public string FrontShiny;
        public string BackShiny;
    }

    public class PokemonMove
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


    public class PokemonTrainer
    {
        public int Rank { get; set; }
        public long ID { get; set; }
        public long TotalExp { get; set; }
        public PokemonSprite TopPokemon { get; set; }
        public string RankString { get { return ": <@" + ID + "> **Total XP:** *" + TotalExp + "* **Top Pokemon:** *" + TopPokemon.NickName + "* " + TopPokemon.Level; } }
    }

    public class MoveList : List<PokemonMove>
    {
        public PokemonMove this[string Name]
        {
            get { return this.Where(x => x.Name == Name).ToList().FirstOrDefault(); }
        }
        public void AddIfNotNull(PokemonMove Move)
        {
            if (Move != null) this.Add(Move);
        }
    }

    public class SpeciesList : List<PokemonSpecies>
    {
        public PokemonSpecies this[int id]
        {
            get { return this.Where(x => x.ID == id).FirstOrDefault(); }
        }
    }

    public class PokemonLearnMoves
    {
        [JsonProperty("ID")]
        public int ID;
        [JsonProperty("Name")]
        public string Name;
        [JsonProperty("LearnLevel")]
        public int LearnLevel;

        public PokemonLearnMoves(int id, string name, int learnlevel)
        {
            ID = id;
            Name = name;
            LearnLevel = learnlevel;
        }
    }

    public class PkmExpClass
    {
        public int AttackerId;
        public int DamageDone;
    }

    public class RewardType
    {
        public string RewardValue { get; set; }
        public string EvolutionText { get; set; }
    }


    
}