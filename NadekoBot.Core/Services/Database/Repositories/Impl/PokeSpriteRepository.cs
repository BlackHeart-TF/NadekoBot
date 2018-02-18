using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class PokeSpriteRepository : Repository<PokemonSprite>, IPokeSpriteRepository
    {
        public PokeSpriteRepository(DbContext context) : base(context) { }
    }
}
