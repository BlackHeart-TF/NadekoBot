using System;
using System.Collections.Generic;
using System.Text;
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
        public int number { get; set; }
        public string name { get; set; }
        public int baseExperience { get; set; }
        public Dictionary<string, int> baseStats { get; set; }
        public int evolveLevel { get; set; }
        public string evolveTo { get; set; }
        public string[] types { get; set; }
        public Dictionary<string, string> moves { get; set; }
        public string imageLink { get; set; }

        public string GetTypeString()
        {
            string types = "";
            foreach (var type in this.types)
                types += type + "/";
            types = types.TrimEnd('/');
            return types;
        }
    }
}
