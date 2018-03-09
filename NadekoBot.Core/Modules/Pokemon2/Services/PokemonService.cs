using NLog;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NadekoBot.Modules.Pokemon2.Common;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Pokemon2.Services
{
    public class PokemonService : INService
    { 
        public readonly List<PokemonSpecies> pokemonClasses = new List<PokemonSpecies>();
        public readonly List<PokemonType> pokemonTypes = new List<PokemonType>();

        public const string PokemonClassesFile = "data/pokemon/pokemonBattlelist.json";
        public const string PokemonTypesFile = "data/pokemon_types.json";

        private Logger _log { get; }
        
        public PokemonService()
        {
            _log = LogManager.GetCurrentClassLogger();
              
            if (File.Exists(PokemonClassesFile))
            {
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                            {
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    System.Diagnostics.Debugger.Break();
                                }
                            }
                };
                pokemonClasses = JsonConvert.DeserializeObject<List<PokemonSpecies>>(File.ReadAllText(PokemonClassesFile),settings);
            }
            else
            {
                pokemonClasses = new List<PokemonSpecies>();
                _log.Warn(PokemonClassesFile + " is missing. Pokemon Classes not loaded.");
            }
            if (File.Exists(PokemonTypesFile))
            {
                pokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                pokemonTypes = new List<PokemonType>();
                _log.Warn(PokemonClassesFile + " is missing. Pokemon types not loaded.");
            }
        }
    }
}
