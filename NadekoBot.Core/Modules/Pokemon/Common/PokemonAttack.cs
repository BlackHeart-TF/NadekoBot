using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Pokemon.Common;
using NadekoBot.Modules.Pokemon.Extentions;
using NadekoBot.Modules.Pokemon.Services;

namespace NadekoBot.Modules.Pokemon.Common
{
    class PokemonAttack
    {
        public PokemonSprite Attacker;
        public PokemonSprite Defender;
        public PokemonSpecies AttackSpecies;
        public PokemonSpecies DefendSpecies;
        public List<PokemonType> AttackerTypes { get; set; }
        public List<PokemonType> DefenseTypes { get; set; }
        public int Damage { get; }
        public PokemonMove Move { get; }
        Random Rng { get; set; } = new Random();
        public bool IsCritical { get; set; } = false;
        public bool MoveHits { get; } 
        /// <summary>
        /// How effective the move is;
        /// 1: somewhat effective,
        /// less than 1: not effective
        /// more than 1: super effective
        /// </summary>
        public double Effectiveness { get; set; } = 1;
        public PokemonAttack(PokemonSprite attacker, PokemonSprite defender, PokemonMove move)
        {
            Attacker = attacker;
            Defender = defender;
            AttackSpecies = attacker.GetSpecies();
            DefendSpecies = defender.GetSpecies();
            AttackerTypes = AttackSpecies.GetPokemonTypes();
            DefenseTypes = DefendSpecies.GetPokemonTypes();
            this.Move = move;
            MoveHits = DoesItHit();
            Damage = CalculateDamage();
            
        }


        private int CalculateDamage()
        {
            if (!MoveHits)
                return 0;
            //use formula in http://bulbapedia.bulbagarden.net/wiki/Damage
            double attack;
            double defense;
            if (Move.Type == "physical")
            {
                attack = Attacker.Attack;
                defense = Defender.SpecialDefense;
            }
            else
            {
                attack = Attacker.SpecialAttack;
                defense = Defender.Defense;
            }
            double toReturn = (((((2 * (double)Attacker.Level)/5) + 2) *Move.Power * (attack / defense))/50) + 2;
            toReturn = toReturn * GetModifier();
            return (int)Math.Floor(toReturn);
        }

        private double GetModifier()
        {
            var stablist = AttackerTypes.Where(x => x.Name == Move.Type);
            double stab = 1;
            if (stablist.Any())
                stab = 1.5;
            var typeEffectiveness = SetEffectiveness();
            double critical = 1;
            if (Rng.Next(0, 100) < 6.25)
            {
                IsCritical = true;
                critical = 1.5;
            }
            double other = /*rng.NextDouble() * 2*/1;
            double random = (double)Rng.Next(85, 100) / 100;
            double mod = stab * typeEffectiveness * critical * other * random;
            return mod;
        }
        
        private bool DoesItHit()
        {
            if (Move.Accuracy == 0)
                return true;
            else
                return Rng.Next(1, 100) <= Move.Accuracy;
        }

        private double SetEffectiveness()
        {


            var moveTypeString = Move.Type.ToUpperInvariant();
            var moveType = moveTypeString.StringToPokemonType();
            var dTypeStrings = DefenseTypes.Select(x => x.Name);
            var mpliers = moveType.Multipliers.Where(x => dTypeStrings.Contains(x.Type));
            foreach (var mplier in mpliers)
            {
                Effectiveness = Effectiveness * mplier.Multiplication;
            }
            return Effectiveness;
        }



        public string AttackString()
        {
            string str = $"**{Attacker.NickName}** attacked **{Defender.NickName}** with **{Move.Name}**\n";
            if (!MoveHits)
            {
                str += "But it missed!\n";
                return str;
            }
            str += $"{Defender.NickName} received {Damage} damage!\n";
            if (IsCritical)
            {
                str += "It's a critical hit!\n";
            }
            if (Effectiveness > 1)
            {
                str += "It's super effective!\n";
            }
            else if (Effectiveness < 1)
            {
                str += "It's not very effective...\n";
            }

            return str;
        }
    }
}
