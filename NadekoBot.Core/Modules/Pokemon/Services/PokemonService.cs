using NLog;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NadekoBot.Modules.Pokemon.Common;
using Newtonsoft.Json;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Common;
using Discord.WebSocket;
using Discord;

namespace NadekoBot.Modules.Pokemon.Services
{
    public class PokemonService : INService
    { 
        public readonly SpeciesList pokemonClasses = new SpeciesList();
        public readonly List<PokemonType> pokemonTypes = new List<PokemonType>();
        public readonly MoveList pokemonMoves = new MoveList();

        public readonly Dictionary<string, Color> TypeColors = new Dictionary<string, Color>();

        //public const string PokemonClassesFile = "data/pokemon/pokemonBattlelist.json";
        public const string PokemonTypesFile = "data/pokemon_types.json";
        public const string PokemonMovesFile = "data/pokemon/PokemonMoves.json";
        public const string PokemonSpeciesFile = "data/pokemon/PokemonSpecies.json";

        private readonly DiscordSocketClient _client;
        private readonly IBotConfigProvider _bc;
        private readonly CommandHandler _cmd;
        private readonly IImageCache _images;
        private readonly Logger _log;
        private readonly NadekoRandom _rng;
        private readonly ICurrencyService _cs;
        public readonly string TypingArticlesPath = "data/typing_articles3.json";
        private readonly CommandHandler _cmdHandler;
        public static PokemonService pokemonInstance;

        public PokemonService(DiscordSocketClient client, CommandHandler cmd, IBotConfigProvider bc, NadekoBot bot,
            NadekoStrings strings, IDataCache data, CommandHandler cmdHandler,
            ICurrencyService cs)
        {
            _client = client;
            _bc = bc;
            _cmd = cmd;
            _images = data.LocalImages;
            _cmdHandler = cmdHandler;
            _log = LogManager.GetCurrentClassLogger();
            _rng = new NadekoRandom();
            _cs = cs;
            pokemonInstance = this;

            LoadColors();
            if (File.Exists(PokemonTypesFile))
            {
                pokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                pokemonTypes = new List<PokemonType>();
                _log.Warn(PokemonTypesFile + " is missing. Pokemon types not loaded.");
            }
            if (File.Exists(PokemonMovesFile))
            {
                pokemonMoves = JsonConvert.DeserializeObject<MoveList>(File.ReadAllText(PokemonMovesFile));
            }
            else
            {
                pokemonMoves = new MoveList();
                _log.Warn(PokemonMovesFile + " is missing. Pokemon types not loaded.");
            }
            if (File.Exists(PokemonSpeciesFile))
            {
                pokemonClasses = JsonConvert.DeserializeObject<SpeciesList>(File.ReadAllText(PokemonSpeciesFile));
            }
            else
            {
                pokemonClasses = new SpeciesList();
                _log.Warn(PokemonSpeciesFile + " is missing. Pokemon types not loaded.");
            }
        }
        public string GetRandomTrainerImage()
        {
            
            var rng = new NadekoRandom();
            var cur = _images.ImageUrls.PokemonUrls;
            return cur[rng.Next(0, cur.Length)];
        }
        public string GetRandomNurseImage()
        {

            var rng = new NadekoRandom();
            var cur = _images.ImageUrls.NurseJoy;
            return cur[rng.Next(0, cur.Length)];
        }

        public IUser GetUserByID(long UserID)
        {
            return _client.GetUser((ulong)UserID);
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
    }
}
