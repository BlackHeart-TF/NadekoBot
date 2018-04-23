using System;
using System.Collections.Generic;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Common.Attributes;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Modules.Pokemon.Services;
using System.Linq;
using NadekoBot.Modules.Pokemon.Extentions;
using NadekoBot.Modules.Pokemon.Common;
using System.Collections.Concurrent;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Pokemon
{
    public class PokemonNew : NadekoTopLevelModule<PokemonService>
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        


        public PokemonNew(DbService db, ICurrencyService cs)
        {
            _db = db;
            _cs = cs;
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show Pokemon QuickHelp")]
        public async Task phelp()
        {
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle("Pokemon Commands:")
                .WithDescription(@"
**.list** *Shows your current party*
**.ml** *Shows your active pokemon's moves*
**.allmoves** *DMs you a full list of your pokemon moves*
**.active** *Gives details on the active pokemon (.active @user)*
**.heal** *Heals a pokemon costs 1* " + _bc.BotConfig.CurrencySign + @"
**.switch name** *Switches to the specified pokemon*
**.rename newName** *Renames your active pokemon to newName*"));
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Shows the top ranking")]
        public async Task elite4(IGuildUser target = null)
        {
            var top = GetTopPlayers();
            string output = "";
            for (int i = 1; i <= top.Count(); i++)
                output += i + top[i-1].RankString + "\n";
            await ReplyAsync(output);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Get the pokemon of someone|yourself")]
        public async Task Active(IGuildUser target = null)
        {
            if (target == null)
                target = (IGuildUser)Context.User;
            var active = ActivePokemon(target);

            //await ReplyAsync($"**{target.Mention}**:\n{active.PokemonString()}");
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(active.NickName.ToTitleCase())
                            .WithDescription("**Species:** " + active.GetSpecies().name + " **Owner:** " + target.Mention)
                            .AddField(efb => efb.WithName("**Stats**").WithValue("\n**Level:** " + active.Level + "\n**HP:** " + active.HP + "/" + 
                                active.MaxHP + "\n**XP:** " + active.XP + "/" + active.XPRequired() + "\n**Type:** "+ active.GetSpecies().GetTypeString()).WithIsInline(true))
                            .AddField(efb => efb.WithName("**Moves**").WithValue(string.Join('\n', active.PokemonMoves())).WithIsInline(true))
                            .WithImageUrl(active.GetSpecies().imageLink));
                            //.AddField(efb => efb.WithName(GetText("height_weight")).WithValue(GetText("height_weight_val", p.HeightM, p.WeightKg)).WithIsInline(true))
                            //.AddField(efb => efb.WithName(GetText("abilities")).WithValue(string.Join(",\n", p.Abilities.Select(a => a.Value))).WithIsInline(true)));
            return;
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task ML()
        {
            var target = (IGuildUser)Context.User;
            var active = ActivePokemon(target);
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithThumbnailUrl(active.GetSpecies().imageLink)
                .AddField(efb => efb.WithName($"**{active.NickName.ToTitleCase()}'s moves**:").WithValue(active.PokemonMoves()).WithIsInline(true)));
            
        }

        [NadekoCommand, Usage, Description, Alias("am")]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task allmoves()
        {
            var target = (IGuildUser)Context.User;
            var pokemon = PokemonList(target);
            string output = "";
            foreach (var pkm in pokemon)
            {
                output += $"**{pkm.NickName}'s moves**:\n{pkm.PokemonMoves()}\n\n";
            }
            await target.SendMessageAsync(output);
            await ReplyAsync(target.Mention + " I sent you a list of all your pokemon and their moves.");

        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task heal(IGuildUser target = null)
        {
            if (target == null)
                target = (IGuildUser)Context.User;
            var pkm = ActivePokemon(target);
            if (pkm.HP == pkm.MaxHP)
            {
                await ReplyAsync($"{ pkm.NickName} is already at full health!");
                return;
            }
            if (_cs.RemoveAsync(Context.User.Id, "Healed a pokemon", 1).Result)
            {
                UpdatePokemon(pkm.Heal());
                await ReplyAsync($"**{ActivePokemon(target).NickName}** has been healed for 1 {_bc.BotConfig.CurrencySign}!");
            }
            else
                await ReplyAsync("You need 1 point to heal");
        }
        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task healall(IGuildUser target = null)
        {
            if (target == null)
                target = (IGuildUser)Context.User;
            var toheal = PokemonList(target).Where(x => x.HP < x.MaxHP);
            var count = toheal.Count();
                if (_cs.RemoveAsync(Context.User.Id,"Healed all pokemon",count).Result)
                foreach(var pkm in toheal)
                {
                    UpdatePokemon(pkm.Heal());
                }
            await ReplyAsync(count + " Pokemon healed for " + count + _bc.BotConfig.CurrencySign + "!");
            
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task heal(string target = null)
        {
            var pkm = PokemonList((IGuildUser)Context.User).Where(x => x.NickName == target).DefaultIfEmpty(null).FirstOrDefault();
            if (pkm.HP == pkm.MaxHP)
            {
                await ReplyAsync($"{ pkm.NickName} is already at full health!");
                return;
            }
            if (_cs.RemoveAsync(Context.User.Id, "Healed a pokemon", 1).Result)
            {
                UpdatePokemon(pkm.Heal());
                await ReplyAsync($"**{pkm.NickName}** has been healed for 1 {_bc.BotConfig.CurrencySign}!");
            }
            else
                await ReplyAsync("You need 1 point to heal");
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Switches the active pokemon")]
        public async Task Switch(string name)
        {
            var target = (IGuildUser)Context.User;
            var list = PokemonList(target);
            var newpkm = list.Where(x => x.NickName == name.Trim()).DefaultIfEmpty(null).FirstOrDefault();
            var trainer = ((IGuildUser)Context.User).GetTrainerStats();
            if (trainer.MovesMade > 0)
            {
                await ReplyAsync("You can't do that right now.");
                return;
            }
            SwitchPokemon(target, newpkm);
            trainer.MovesMade++;
            target.UpdateTrainerStats(trainer);
            await ReplyAsync($"Switched to **{newpkm.NickName}**");

        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public async Task Attack([Summary("The User to target")] IGuildUser target, [Remainder] string moveString)
        {
            var attackerPokemon = ActivePokemon((IGuildUser)Context.User);
            var species = attackerPokemon.GetSpecies();
            if (!species.moves.Keys.Contains(moveString.Trim()))
            {
                await ReplyAsync($"Cannot use \"{moveString}\", see `{Prefix}ML` for moves");
                return;
            }
            var attackerStats = ((IGuildUser)Context.User).GetTrainerStats();
            var defenderStats = target.GetTrainerStats();
            if (attackerStats.MovesMade > TrainerStats.MaxMoves || attackerStats.LastAttacked.Contains(target.Id))
            {
                await ReplyAsync($"{Context.User.Mention} already attacked {target.Mention}!");
                return;
            }
            if (attackerPokemon.HP == 0)
            {
                await ReplyAsync($"{attackerPokemon.NickName} has fainted and can't attack!");
                return;
            }
            
            KeyValuePair<string, string> move = new KeyValuePair<string, string>(moveString, species.moves[moveString]);
            var defenderPokemon = ActivePokemon(target);

            if (defenderPokemon.HP == 0)
            {
                await ReplyAsync($"{defenderPokemon.NickName} has already fainted!");
                return;
            }

            PokemonAttack attack = new PokemonAttack(attackerPokemon, defenderPokemon, move);
            var msg = attack.AttackString();
            
            defenderPokemon.HP -= attack.Damage;
            msg += $"{defenderPokemon.NickName} has {defenderPokemon.HP} HP left!";
            await ReplyAsync(msg);
            //Update stats, you shall
            ((IGuildUser)Context.User).UpdateTrainerStats(attackerStats.Attack(target));
            target.UpdateTrainerStats(defenderStats.Reset());
            UpdatePokemon(attackerPokemon);
            UpdatePokemon(defenderPokemon);

            if (defenderPokemon.HP <= 0)
            {
                
                var str = $"{defenderPokemon.NickName} fainted!\n{attackerPokemon.NickName}'s owner {Context.User.Mention} receives 1 point\n";
                var lvl = attackerPokemon.Level;
                var extraXP = attackerPokemon.Reward(defenderPokemon);
                str += $"{attackerPokemon.NickName} gained {extraXP} XP from the battle\n";
                if (attackerPokemon.Level > lvl) //levled up
                {
                    str += $"**{attackerPokemon.NickName}** leveled up!\n**{attackerPokemon.NickName}** is now level **{attackerPokemon.Level}**";
                    //Check evostatus
                }
                UpdatePokemon(attackerPokemon);
                UpdatePokemon(defenderPokemon);
                var list = PokemonList(target).Where(s => (s.HP > 0 && s != defenderPokemon));
                if (list.Any())
                {
                    var toSet = list.FirstOrDefault();
                    switch (SwitchPokemon(target, toSet))
                    {
                        case 0:
                            {
                                str += $"\n{target.Mention}'s active pokemon set to **{toSet.NickName}**";
                                break;
                            }
                        case 1:
                        case 2:
                            {
                                str += $"\n **Error:** could not switch pokemon";
                                break;
                            }
                    }
                }
                else
                {
                    str += $"\n{target.Mention} has no pokemon left!";
                    //do something?
                }
                //UpdatePokemon(attackerPokemon);
                //UpdatePokemon(defenderPokemon);
                await ReplyAsync(str);
                await _cs.AddAsync(Context.User.Id, "Victorious in pokemon", 1);
            }

        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task rename([Remainder] string name)
        {
            var target = (IGuildUser)Context.User;
            var active = ActivePokemon(target);
            var output = "**" + active.NickName + "** renamed to **";
            active.Rename(name);
            UpdatePokemon(active);
            
            await ReplyAsync(output + active.NickName + "**");

        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Shows your current party")]
        public async Task List()
        {
            var list = PokemonList((IGuildUser)Context.User);
            string str = $"{Context.User.Mention}'s pokemon are:\n";
            foreach (var pkm in list)
            {
                if (pkm.IsActive)
                {
                    str += $"__**{pkm.NickName}** : *{pkm.GetSpecies().name}*  HP: {pkm.HP}/{pkm.MaxHP}__\n";
                }
                else if (pkm.HP == 0)
                {
                    str += $"~~**{pkm.NickName}** : *{pkm.GetSpecies().name}*  HP: {pkm.HP}/{pkm.MaxHP}~~☠\n";
                }
                else
                {
                    str += $"**{pkm.NickName}** : *{pkm.GetSpecies().name}*  HP: {pkm.HP}/{pkm.MaxHP}\n";
                }

            }
            await ReplyAsync(str);
        }
        /// <summary>
        /// Sets the active pokemon of the given user to the given Sprite
        /// </summary>
        /// <param name="u"></param>
        /// <param name="newActive"></param>
        /// <returns></returns>
        int SwitchPokemon(IGuildUser u, PokemonSprite newActive)
        {
            var toUnset = PokemonList(u).Where(x => x.IsActive).FirstOrDefault();
            if (toUnset == null)
            {
                return 1;
            }
            if (newActive.HP <= 0)
            {
                return 2;
            }
            toUnset.IsActive = false;
            newActive.IsActive = true;
            UpdatePokemon(toUnset);
            UpdatePokemon(newActive);
            
            return 0;
        }
        public async void UpdatePokemon(PokemonSprite pokemon)
        {
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Update(pokemon);
            await uow.CompleteAsync();
        }
        public PokemonSprite ActivePokemon(IGuildUser u)
        {
            var list = PokemonList(u);
            return list.Where(x => x.IsActive).FirstOrDefault();
        }

        List<PokemonSprite> PokemonList(IGuildUser u)
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var row = db.Where(x => x.OwnerId == (long)u.Id);
            if (row.Count() >= 6)
            {
                return row.ToList();
            }
            else
            {

                var list = new List<PokemonSprite>();
                while (list.Count < 6)
                {
                    var pkm = GeneratePokemon(u);
                    if (!list.Where(x => x.IsActive).Any())
                    {
                        pkm.IsActive = true;
                    }

                    list.Add(pkm);
                    
                    
                }
                //Set an active pokemon
                var uow = _db.UnitOfWork;
                uow.PokemonSprite.AddRange(list.ToArray());
                uow.CompleteAsync();

                return list;
            }
        }

        List<PokemonTrainer> GetTopPlayers()
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var users = db.DistinctBy(x => x.OwnerId);
            var output = new List<PokemonTrainer>();
            foreach (var user in users)
            {
                var usrpkm = db.Where(x => x.OwnerId == user.OwnerId);
                long exp = 0;
                foreach (var pkm in usrpkm)
                    exp += pkm.XP;
                var trainer = new PokemonTrainer()
                {
                    ID = user.OwnerId,
                    TotalExp = exp,
                    TopPokemon = usrpkm.OrderByDescending(x => x.Level).First()
                };
                output.Add(trainer);
            }
            output = output.OrderByDescending(x => x.TotalExp).Take(4).ToList();
            return output;
        }

        Random rng = new Random();
        private PokemonSprite GeneratePokemon(IGuildUser u)
        {

            var list =_service.pokemonClasses.Where(x => x.evolveLevel != -1).ToList();
            var speciesIndex = rng.Next(0, list.Count() - 1);
            rng.Next();
            var species = list[speciesIndex];

            PokemonSprite sprite = new PokemonSprite
            {
                SpeciesId = species.number,
                HP = species.baseStats["hp"],
                Level = 1,
                NickName = species.name,
                OwnerId = (long)u.Id,
                XP = 0,
                Attack = species.baseStats["attack"],
                Defense = species.baseStats["defense"],
                SpecialAttack = species.baseStats["special-attack"],
                SpecialDefense = species.baseStats["special-defense"],
                Speed = species.baseStats["speed"],
                MaxHP = species.baseStats["hp"]
            };

            while (sprite.Level < 4)
            {
                sprite.LevelUp();
            }
            sprite.XP = sprite.XPRequired();
            sprite.LevelUp();
            return sprite;
        }
    }
}
