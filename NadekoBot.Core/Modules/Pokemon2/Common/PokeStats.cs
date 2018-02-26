using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Modules.Pokemon2.Common
{
    public class PokeStats
    {
        //Health left
        public int Hp { get; set; } = 500;
        public int MaxHp { get; } = 500;
        //Amount of moves made since last time attacked
        public int MovesMade { get; set; } = 0;
        //Last people attacked
        public List<ulong> LastAttacked { get; set; } = new List<ulong>();
    }
    public class TrainerStats
    {

        public static int MaxMoves { get; } = 5;
        /// <summary>
        /// Amount of moves made since last time attacked
        /// </summary>
        public int MovesMade { get; set; } = 0;
        /// <summary>
        /// Last people attacked
        /// </summary>
        public List<ulong> LastAttacked { get; set; } = new List<ulong>();

        public TrainerStats Attack(IGuildUser user)
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
