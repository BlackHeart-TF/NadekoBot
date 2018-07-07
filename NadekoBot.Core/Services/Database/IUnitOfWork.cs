﻿using NadekoBot.Core.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        NadekoContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        IBotConfigRepository BotConfig { get; }
        ICustomReactionRepository CustomReactions { get; }
        ICurrencyTransactionsRepository CurrencyTransactions { get; }
        IMusicPlaylistRepository MusicPlaylists { get; }
        IPokeSpriteRepository PokemonSprite { get; }
        IPokeGameRepository PokeGame { get; }
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }
        IWarningsRepository Warnings { get; }
        IXpRepository Xp { get; }
        IClubRepository Clubs { get; }
        IPollsRepository Polls { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
