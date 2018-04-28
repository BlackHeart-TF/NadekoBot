using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Modules.Pokemon.Common
{
    public class TrainerStats
    {
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

        public TrainerStats Attack(IUser user)
        {
            LastAttacked.Add(user.Id);
            MovesMade++;
            return this;
        }

        public TrainerStats Reset()
        {
            LastAttacked = new List<ulong>();
            MovesMade = 0;
            return this;
        }
    }

}
