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
        public int DrainDamage { get; }
        public string StatusApplied { get; }
        public int StatusTurns { get; }
        public int StatusDamage { get; }
        private bool Thawed { get; }
        private bool Woken { get; }
        private bool FullyParalyzed { get; }
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
            Woken = DidItWake();
            Thawed = DidItThaw();
            if (Thawed || Woken)
                Attacker.StatusEffect = "none";
            if (Attacker.StatusTurns > 0)
                Attacker.StatusTurns--;
            FullyParalyzed = IsFullyParalyzed();
            MoveHits = DoesItHit();
            Damage = MoveHits ? CalculateDamage() : 0;
            StatusApplied = MoveHits ? CalculateStatus() : "none";
            StatusTurns = MoveHits ? CalcStatusTurns() : 0;
            DrainDamage = CalculateDrainDamage();
            StatusDamage = CalculateStatusDamage();
        }
        public void Commit()
        {
            if (MoveHits)
            {
                Defender.HP -= Damage;
                if (StatusApplied != "none")
                {
                    Defender.StatusEffect = StatusApplied;
                    Defender.StatusTurns = (int)StatusTurns;
                }
            }
            Attacker.HP -= StatusDamage;
            Attacker.HP += DrainDamage;
            if (Thawed)
                Attacker.StatusEffect = "none";
            
            Attacker.Update();
            Defender.Update();
        }

        private string CalculateStatus()
        {
            Defender.StatusEffect ??="none";

            if (Defender.StatusEffect != "none")
                return "none";
            var ME = Move.MoveEffects;
            if (ME.Ailment != "none")
            {
                if (ME.AilmentChance == 0 || (Rng.Next(0, 100) < ME.AilmentChance))
                {
                    return ME.Ailment;
                }
            }
            return "none";
        }

        private int CalcStatusTurns()
        {
            if (Move.MoveEffects.MaxTurns != null)
                return Rng.Next(1,(int)Move.MoveEffects.MaxTurns);
            return 0;
        }

        private int CalculateDrainDamage()
        {
            return (int)Math.Round(Damage * ((double)Move.MoveEffects.Drain / 100));
        }

        private int CalculateStatusDamage()
        {
            switch (Attacker.StatusEffect)
            {
                case "burn":
                    return Attacker.HP / 16+1;

                case "poison":
                    return Attacker.HP / 16+1;

                default:
                    return 0;

            }
        }

        private int CalculateDamage()
        {
            if (!MoveHits)
                return 0;
            if (Move.DamageType == "status")
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
            if (Attacker.StatusEffect == "sleep")
                return false;
            if (FullyParalyzed)
                return false;
            if(Attacker.StatusEffect == "freeze" && Thawed==false)
            {
                return false;
            }
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

        private bool IsFullyParalyzed()
        {
            if (Attacker.StatusEffect == "paralysis") {
                var rand = Rng.Next(1, 5);
                if (rand == 2)
                    return true;
            }
                
            return false;
        }

        private bool DidItWake()
        {
            if (Attacker.StatusEffect == "sleep" && Attacker.StatusTurns == 0)
            {
                return true;
            }
            return false;
        }

        private bool DidItThaw()
        {
            if (Attacker.StatusEffect == "freeze" &&(Attacker.StatusTurns == 0 || Rng.Next(0, 100) < 20))
            {
                return true;
            }
            return false;
        }

        public string AttackString()
        {
            string str = "";
            if (Attacker.StatusEffect == "paralysis")
                str += $"**{Attacker.NickName}** is paralyzed!\n";
            if (FullyParalyzed)
            {
                str += $"It can't move!\n";
                return str;
            }
            if (Attacker.StatusEffect == "freeze")
            {
                str += $"**{Attacker.NickName}** is frozen solid!\n";
                return str;
            }
            if (Thawed)
                str += $"**{Attacker.NickName}** has thawed out!\n";

            if (Attacker.StatusEffect == "sleep")
            {
                str += $"💤 **{Attacker.NickName}** is fast asleep!\n";
                return str;
            }
            if (Woken)
                str += $"**{Attacker.NickName}** woke up!\n";

            str += $"**{Attacker.NickName}** attacked **{Defender.NickName}** with **{Move.Name}**\n";
            if (!MoveHits)
            {
                str += "But it missed!\n";
            }
            if (Damage > 0)
            {
                str += $"**{Defender.NickName}** received {Damage} damage!\n";
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
            }

            if(DrainDamage < 0)
                str += $"**{Attacker.NickName}** was hit by recoil!\n";
            else if (DrainDamage > 0)
                str += $"**{Attacker.NickName}** drained energy and restored {DrainDamage} HP!\n";

            switch (StatusApplied)
            {
                case "burn":
                    str += $"The opponent's {Defender.NickName} it burned! 🔥\n";
                    break;

                case "paralysis":
                    str += $"The opponent's {Defender.NickName} is paralyzed. It can't attack!\n";
                    break;

                case "poison":
                    str += $"The opponent's {Defender.NickName} has been poisoned!\n";
                    break;

                case "sleep":
                    str += $"The opponent's {Defender.NickName} fell asleep! 💤\n";
                    break;

                case "freeze":
                    str += $"The opponent's {Defender.NickName} was forzen solid! 🌬️\n";
                    break;

                default:
                    break;
            }

            str += $"{Defender.NickName} has {Defender.HP} HP left!\n";
            if (Attacker.StatusEffect == "burn" || Attacker.StatusEffect == "poison")
            {
                str += $"**{Attacker.NickName}** took {StatusDamage} damage from it's {Attacker.StatusEffect}\n";
                str += $"{Attacker.NickName} has {Attacker.HP} HP left!";
            }

            return str;
        }
    }
}
