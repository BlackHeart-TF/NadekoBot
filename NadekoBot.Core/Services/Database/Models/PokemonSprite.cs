namespace NadekoBot.Core.Services.Database.Models
{
    public class PokemonSprite : DbEntity
    {
        public long OwnerId { get; set; }
        public string NickName { get; set; }
        public int HP { get; set; }
        public long XP { get; set; }
        public int Level { get; set; }
        public int SpeciesId { get; set; }
        public bool IsActive { get; set; }

        //stats
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int MaxHP { get; set; }
        public int Speed { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            PokemonSprite other = obj as PokemonSprite;


            return (OwnerId == other.OwnerId) && (NickName == other.NickName) &&
                (Level == other.Level) && (SpeciesId == other.SpeciesId) &&
                (IsActive == other.IsActive) && (Attack == other.Attack) && (Speed == other.Speed);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
