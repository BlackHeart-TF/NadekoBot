using Discord;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Pokemon.Extentions;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Modules.Pokemon.Common
{
    public class TrainerStats
    {
        public IUser Trainer;
        public Dictionary<ulong,IUser> LastAttackedBy { get; set; } = new Dictionary<ulong,IUser>();
        public static int MaxMoves { get; } = 5;
        /// <summary>
        /// Amount of moves made since last time attacked
        /// </summary>
        public int MovesMade { get; set; } = 0;
        /// <summary>
        /// Last people attacked
        /// </summary>
        public List<ulong> LastAttacked { get; set; } = new List<ulong>();

        public Dictionary<int, Dictionary<int, int>> AttackerDamage = new Dictionary<int, Dictionary<int, int>>(); //<YourPkmId,<theirpkmId,damageDone>>

        public TrainerStats(IUser user)
        {
            Trainer = user;
        }

        public TrainerStats Attack(IUser user, int damage)
        {
            var userID = Trainer.ActivePokemon().Id;
            LastAttacked.Add(user.Id);
            AttackerDamage.TryAdd(userID, new Dictionary<int, int>());

            if (!AttackerDamage[userID].TryAdd(user.ActivePokemon().Id, damage))
                AttackerDamage[userID][user.ActivePokemon().Id] += damage;
            MovesMade++;
            return this;
        }

        public TrainerStats Reset()
        {
            LastAttacked = new List<ulong>();
            MovesMade = 0;
            return this;
        }

        public Dictionary<int,int> ExpPercentage(PokemonSprite Sprite)
        {
            var attackers = new Dictionary<int, int>();
            AttackerDamage.TryGetValue(Sprite.Id, out attackers);

            var DamagePercentage = new Dictionary<int, int>();
            foreach (var enemy in attackers)
            {
                var percentage = enemy.Value / Sprite.MaxHP;
                DamagePercentage.Add(enemy.Key, percentage);
            }
            return DamagePercentage;
        }
    }

}
