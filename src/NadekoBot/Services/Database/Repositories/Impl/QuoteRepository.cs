﻿using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword) => 
            _set.Where(q => q.GuildId == guildId && q.Keyword == keyword);

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new NadekoRandom();
            return _set.Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next()).FirstOrDefaultAsync();
        }
    }
}
