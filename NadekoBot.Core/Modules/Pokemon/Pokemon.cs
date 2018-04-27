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
        public async Task Phelp()
        {
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle("Pokemon Commands:")
                .WithDescription(@"
**.list** *Shows your current party*
**.ml** *Shows your active pokemon's moves*
**.allmoves** *DMs you a full list of your pokemon moves*
**.active** *Gives details on the active pokemon (.active @user)*
**.heal** *Heals a pokemon costs 1* " + _bc.BotConfig.CurrencySign + @"
**.healall** *Heals your party. Costs 1" + _bc.BotConfig.CurrencySign + @" per pokemon*
**.nursejoy** *Heals your party once they have all fainted (only if you are too broke to .healall)*
**.switch name** *Switches to the specified pokemon*
**.rename newName** *Renames your active pokemon to newName*
**.elite4** *Shows the top 4 players and their best pokemon*
**.rank** *Shows your pokemon ranking (.rank @user)*
**.catch @botName pokemonToReplace** *replaces the specified pokemon with one of the bots pokemon*"));
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Shows the top ranking")]
        public async Task Elite4()
        {
            var top = GetTopPlayers();
            string output = "";
            for (int i = 1; i <= top.Count(); i++)
                output += i + top[i-1].RankString + "\n";
            await ReplyAsync(output);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Shows the top ranking")]
        public async Task Rank(IGuildUser target = null)
        {
            if (target == null)
                target = (IGuildUser)Context.User;

            var top = GetPlayerRank(target);
            string output = "";
            for (int i = 1; i <= top.Count(); i++)
                output += top[i-1].Rank + top[i - 1].RankString + "\n";
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
                            .WithDescription("**Species:** " + active.GetSpecies().Name + " **Owner:** " + target.Mention)
                            .AddField(efb => efb.WithName("**Stats**").WithValue("\n**Level:** " + active.Level + "\n**HP:** " + active.HP + "/" + 
                                active.MaxHP + "\n**XP:** " + active.XP + "/" + active.XPRequired() + "\n**Type:** "+ active.GetSpecies().GetTypeString()).WithIsInline(true))
                            .AddField(efb => efb.WithName("**Moves**").WithValue(string.Join('\n', active.PokemonMoves())).WithIsInline(true))
                            .WithImageUrl(active.GetSpecies().ImageLink));
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
                .WithThumbnailUrl(active.GetSpecies().ImageLink)
                .AddField(efb => efb.WithName($"**{active.NickName.ToTitleCase()}'s moves**:").WithValue(active.PokemonMoves()).WithIsInline(true)));
            
        }

        [NadekoCommand, Usage, Description, Alias("catch")]
        [RequireContext(ContextType.Guild)]
        [Summary("replaces your selected pokemon with a wild")]
        public async Task CatchPkm(IGuildUser target, string pokemon)
        {
            var pkmList = PokemonList((IGuildUser)Context.User);
            var pokemonNumber = pkmList.IndexOf(pkmList.Where(x => x.NickName == pokemon).DefaultIfEmpty(null).FirstOrDefault()) +1;
            await CatchPkm(target, pokemonNumber);
        }

        [NadekoCommand, Usage, Description, Alias("catch")]
        [RequireContext(ContextType.Guild)]
        [Summary("replaces your selected pokemon with a wild")]
        public async Task CatchPkm(IGuildUser target, int slot)
        {
            if (!target.IsBot)
            {
                var embed = new EmbedBuilder().WithColor(Color.Purple)
                    .WithDescription("That's not a wild pokemon!")
                    .WithImageUrl(_service.GetRandomTrainerImage()).Build();
                await ReplyAsync("", false, embed);
                return;
            }
            if (!_cs.RemoveAsync(Context.User.Id, "Dropped a ball", 1).Result)
            {
                await ReplyAsync($"Not enough {_bc.BotConfig.CurrencySign}!");
                return;
            }


            var targetPkm = ActivePokemon(target);
            var replacedPkm = PokemonList((IGuildUser)Context.User)[slot - 1];

            int ballchanceN =  rng.Next(0, 255);
            int catchRate = 195;
            if (ballchanceN > catchRate)
            {
                await ReplyAsync(Context.User.Mention + "The Pokemon broke free!");
                return;
            }
            int M = rng.Next(0, 255);
            var catchChance = Math.Round((decimal)((targetPkm.MaxHP * 255 * 4) / (targetPkm.HP * 12)));

            if (catchChance < M)
            {
                await ReplyAsync(Context.User.Mention + "The Pokemon broke free!");
                return;
            }
            DeletePokemon(targetPkm);
            targetPkm.Id = replacedPkm.Id;
            targetPkm.OwnerId = replacedPkm.OwnerId;
            targetPkm.IsActive = replacedPkm.IsActive;
            UpdatePokemon(targetPkm);
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Add(GeneratePokemon(target));
            await uow.CompleteAsync();


            await ReplyAsync($"**{replacedPkm.NickName}** released!\n Caught **{targetPkm.NickName}**!");
        }

        [NadekoCommand, Usage, Description, Alias("am")]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of all your pokemon")]
        public async Task AllMoves()
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
        [Summary("Heals the specified users active pokemon (default self)")]
        public async Task Heal(IGuildUser target = null)
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
        [Summary("Heals all pokemon of the specified user (default self)")]
        public async Task Healall(IGuildUser target = null)
        {
            if (target == null)
                target = (IGuildUser)Context.User;
            var toheal = PokemonList(target).Where(x => x.HP < x.MaxHP);
            var count = toheal.Count();
            if (_cs.RemoveAsync(Context.User.Id, "Healed all pokemon", count).Result)
            {
                foreach (var pkm in toheal)
                {
                    UpdatePokemon(pkm.Heal());
                }
                await ReplyAsync(count + " Pokemon healed for " + count + _bc.BotConfig.CurrencySign + "!");
            }
            else
                await ReplyAsync(Context.User.Mention + ", you do not have enough " + _bc.BotConfig.CurrencySign + ". You need " + count);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task Heal(string target = null)
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
        [Summary("Show the moves of the active pokemon")]
        public async Task NurseJoy()
        {
                var target = (IGuildUser) Context.User;
            long currency;
            using (var uow = _db.UnitOfWork)
            {
                 currency = uow.DiscordUsers.GetOrCreate(Context.User).CurrencyAmount;
            }
           
            var toheal = PokemonList(target).Where(x => x.HP == 0);
            if (toheal.Count() == 6)
            {
                if (currency >= 6)
                {
                    await ReplyAsync($"You have enough {_bc.BotConfig.CurrencyName} {_bc.BotConfig.CurrencySign} to heal yourself. Use `.healall`");
                    return;
                }
                foreach (var pkm in toheal)
                {
                    UpdatePokemon(pkm.Heal());
                }
                await ReplyAsync(Context.User.Mention +",\n Your Pokémon are fighting fit!\nWe hope to see you again!");
            }
            else
                await ReplyAsync(Context.User.Mention + ", you still have pokemon willing to fight! Get back in there!");

        }


        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Switches the active pokemon")]
        public async Task Switch(string name, string move = null)
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
            switch (SwitchPokemon(target, newpkm)) { 
                case SwitchResult.TargetFainted:
                    await ReplyAsync(Context.User.Mention + ", " + newpkm.NickName + " has already fainted!");
                    return;
                case SwitchResult.Pass:
                    trainer.MovesMade++;
                    target.UpdateTrainerStats(trainer);
                    await ReplyAsync($"{Context.User.Mention} switched to **{newpkm.NickName}**");
                    break;
                case SwitchResult.Failed:
                    await ReplyAsync("Something went wrong!");
                    return;
            }
            if (move != null)
            {
                if (target.GetTrainerStats().LastAttackedBy == null)
                {
                    await ReplyAsync("Can't attack. Use `.attack @target move`");
                    return;
                }
                await Attack(move).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public async Task Attack([Remainder] string moveString)
        {
            var user = ((IGuildUser)Context.User).GetTrainerStats().LastAttackedBy;
            if (user == null)
            {
                await ReplyAsync("Target a user with `.attack @user move`");
                return;
            }
             
            await Attack(user, moveString);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public async Task Attack([Summary("The User to target")] IGuildUser target, [Remainder] string moveString)
        {
            await DoAttack((IGuildUser)Context.User, target, moveString);

        }

        public async Task DoAttack(IGuildUser attacker, IGuildUser target, [Remainder] string moveString)
        {
            var attackerPokemon = ActivePokemon(attacker);
            var species = attackerPokemon.GetSpecies();
            if (!species.Moves.Keys.Contains(moveString.Trim()))
            {
                await ReplyAsync($"Cannot use \"{moveString}\", see `{Prefix}ML` for moves");
                return;
            }
            var attackerStats = (attacker).GetTrainerStats();
            var defenderStats = target.GetTrainerStats();
            if (attackerStats.MovesMade > TrainerStats.MaxMoves || attackerStats.LastAttacked.Contains(target.Id))
            {
                await ReplyAsync($"{attacker.Mention} already attacked {target.Mention}!");
                return;
            }
            if (attackerPokemon.HP == 0)
            {
                await ReplyAsync($"{attackerPokemon.NickName} has fainted and can't attack!");
                return;
            }
            defenderStats.LastAttackedBy = attacker;
            KeyValuePair<string, string> move = new KeyValuePair<string, string>(moveString, species.Moves[moveString]);
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
            attacker.UpdateTrainerStats(attackerStats.Attack(target));
            target.UpdateTrainerStats(defenderStats.Reset());
            UpdatePokemon(attackerPokemon);
            UpdatePokemon(defenderPokemon);

            if (defenderPokemon.HP <= 0)
            {
                
                var str = $"{defenderPokemon.NickName} fainted!\n{attackerPokemon.NickName}'s owner {attacker.Mention} receives 1 point\n";
                var lvl = attackerPokemon.Level;
                if (!target.IsBot)
                {
                    var extraXP = attackerPokemon.Reward(defenderPokemon);
                    str += $"{attackerPokemon.NickName} gained {extraXP} XP from the battle\n";
                }
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
                        case SwitchResult.Pass:
                            {
                                str += $"\n{target.Mention}'s active pokemon set to **{toSet.NickName}**";
                                break;
                            }
                        case SwitchResult.Failed:
                        case SwitchResult.TargetFainted:
                            {
                                str += $"\n **Error:** could not switch pokemon";
                                break;
                            }
                    }
                }
                else
                {
                    str += $"\n{target.Mention} has no pokemon left!";
                    if (target.IsBot)
                    {
                        var pkmlist = PokemonList(target);
                        foreach (var pkm in pkmlist)
                            UpdatePokemon(pkm.Heal());
                    }
                    //do something?
                }
                //UpdatePokemon(attackerPokemon);
                //UpdatePokemon(defenderPokemon);
                await ReplyAsync(str);
                await _cs.AddAsync(attacker.Id, "Victorious in pokemon", 1);
                
            }
            if (target.IsBot)
            {
                await DoAttack(target, attacker, ActivePokemon(target).GetSpecies().Moves.Keys.ElementAt((new Random().Next(3))));
                
            }
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Summary("Show the moves of the active pokemon")]
        public async Task SwapPokemon(IGuildUser user, string oldpkm, string newpkm, int level = 5)
        {
            await ReplyAsync(swapPokemon(user, oldpkm, newpkm, level));
        }
        

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task Rename([Remainder] string name)
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
                    str += $"__**{pkm.NickName}** : *{pkm.GetSpecies().Name}*  HP: {pkm.HP}/{pkm.MaxHP}__\n";
                }
                else if (pkm.HP == 0)
                {
                    str += $"~~**{pkm.NickName}** : *{pkm.GetSpecies().Name}*  HP: {pkm.HP}/{pkm.MaxHP}~~☠\n";
                }
                else
                {
                    str += $"**{pkm.NickName}** : *{pkm.GetSpecies().Name}*  HP: {pkm.HP}/{pkm.MaxHP}\n";
                }

            }
            await ReplyAsync(str);
        }

        enum SwitchResult{
            Pass,
            Failed,
            TargetFainted,

        }
        /// <summary>
        /// Sets the active pokemon of the given user to the given Sprite
        /// </summary>
        /// <param name="u"></param>
        /// <param name="newActive"></param>
        /// <returns></returns>
        SwitchResult SwitchPokemon(IGuildUser u, PokemonSprite newActive)
        {
            var toUnset = PokemonList(u).Where(x => x.IsActive).FirstOrDefault();
            if (toUnset == null)
            {
                return SwitchResult.Failed;
            }
            if (newActive.HP <= 0)
            {
                return SwitchResult.TargetFainted;
            }
            toUnset.IsActive = false;
            newActive.IsActive = true;
            UpdatePokemon(toUnset);
            UpdatePokemon(newActive);
            
            return SwitchResult.Pass;
        }
        

        public async void UpdatePokemon(PokemonSprite pokemon)
        {
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Update(pokemon);
            await uow.CompleteAsync();
        }

        public async void DeletePokemon(PokemonSprite pokemon)
        {
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Remove(pokemon);
            await uow.CompleteAsync();
        }

        public PokemonSprite ActivePokemon(IGuildUser u)
        {
            var list = PokemonList(u);
            return list.Where(x => x.IsActive).FirstOrDefault();
        }

        string swapPokemon(IGuildUser user, string OldPokemon, string NewPokemon, int Level = 5)
        {
            var oldpkm = _db.UnitOfWork.PokemonSprite.GetAll().Where(x => x.OwnerId==(long)user.Id && x.NickName==OldPokemon).First();
            var newspecies = _service.pokemonClasses.Where(x => x.Name == NewPokemon).DefaultIfEmpty(null).First();
            oldpkm.SpeciesId = newspecies.Number;
            oldpkm.NickName = newspecies.Name;
            oldpkm.Level = 0;
            oldpkm.XP = 0;
            while (oldpkm.Level <= Level-1)
            {
                oldpkm.LevelUp();
            }
            oldpkm.HP = oldpkm.MaxHP;
            UpdatePokemon(oldpkm);
            return "Probably worked. idk, theres no error checking, do `.list` to be sure";
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
                while (row.Count() + list.Count < 6)
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

        List<PokemonTrainer> GetPlayerRank(IGuildUser player)
        {
            var db = _db.UnitOfWork.PokemonSprite.GetAll();
            var users = db.DistinctBy(x => x.OwnerId);
            var output = new List<PokemonTrainer>();
            //int player 
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
            output = output.OrderByDescending(x => x.TotalExp).ToList();
            for (int i = 0; i < output.Count; i++)
                output[i].Rank = i+1;
            var playerRank = output.Where(x => x.ID == (long)player.Id).First().Rank;
            output=output.Skip(playerRank - 3).Take(5).ToList();
            return output;
        }

        Random rng = new Random();
        private PokemonSprite GeneratePokemon(IGuildUser u)
        {

            var list =_service.pokemonClasses.Where(x => x.EvolveLevel != -1).ToList();
            var speciesIndex = rng.Next(0, list.Count() - 1);
            rng.Next();
            var species = list[speciesIndex];

            PokemonSprite sprite = new PokemonSprite
            {
                SpeciesId = species.Number,
                HP = species.BaseStats["hp"],
                Level = 1,
                NickName = species.Name,
                OwnerId = (long)u.Id,
                XP = 0,
                Attack = species.BaseStats["attack"],
                Defense = species.BaseStats["defense"],
                SpecialAttack = species.BaseStats["special-attack"],
                SpecialDefense = species.BaseStats["special-defense"],
                Speed = species.BaseStats["speed"],
                MaxHP = species.BaseStats["hp"]
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
