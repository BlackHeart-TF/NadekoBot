using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{

    public class PokeSpriteRepository : Repository<PokemonSprite>, IPokeSpriteRepository
    {
        public PokeSpriteRepository(DbContext context) : base(context) { }
    }
    
}
