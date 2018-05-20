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
using NadekoBot.Common;
using Discord.WebSocket;

namespace NadekoBot.Modules.Pokemon
{
    public class PokemonNew : NadekoTopLevelModule<PokemonService>
    {
        private static DbService _db;
        private readonly ICurrencyService _cs;
        private NadekoRandom rng = new NadekoRandom();

        public static DbService GetDb()
        {
            return _db;
        }

        public PokemonNew(DbService db, ICurrencyService cs)
        {
            _db = db;
            _cs = cs;
        }

        [NadekoCommand, Usage, Description, Alias]
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
**.catch @botName pokemonToReplace** *replaces the specified pokemon with one of the bots pokemon*
**.learn moveToReplace** *replaces the specified move with one your pokemon is trying to learn from levelling*"));
        }

        [NadekoCommand, Usage, Description, Alias]
        [Summary("Shows the top ranking")]
        public async Task Elite4()
        {
            var top = await PokemonFunctions.GetTopPlayersAsync();
            string output = "";
            for (int i = 1; i <= top.Count(); i++)
                output += i + top[i-1].RankString + "\n";
            await ReplyAsync(output);
        }

        [NadekoCommand, Usage, Description, Alias]
        [Summary("Shows the top ranking")]
        public async Task Rank(IUser target = null)
        {
            if (target == null)
                target = Context.User;

            var top = await PokemonFunctions.GetPlayerRankAsync(target);
            string output = "";
            for (int i = 1; i <= top.Count(); i++)
                output += top[i-1].Rank + top[i - 1].RankString + "\n";
            await ReplyAsync(output);
        }

        [NadekoCommand, Usage, Description, Alias]
        [Summary("Get the pokemon of someone|yourself")]
        public async Task Active(IUser target = null)
        {
            if (target == null)
                target = Context.User;
            var active = target.ActivePokemon();

            //await ReplyAsync($"**{target.Mention}**:\n{active.PokemonString()}");
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(active.NickName.ToTitleCase())
                            .WithDescription("**Species:** " + active.GetSpecies().Name + " **Owner:** " + target.Mention)
                            .AddField(efb => efb.WithName("**Stats**").WithValue("\n**Level:** " + active.Level + "\n**HP:** " + active.HP + "/" + 
                                active.MaxHP + "\n**XP:** " + active.XP + "/" + active.XPRequired() + "\n**Type:** "+ active.GetSpecies().GetTypeString()).WithIsInline(true))
                            .AddField(efb => efb.WithName("**Moves**").WithValue(string.Join('\n', active.PokemonMoves().Result)).WithIsInline(true))
                            .WithImageUrl(active.IsShiny? active.GetSpecies().Sprites.FrontShiny:active.GetSpecies().Sprites.Front));
                            //.AddField(efb => efb.WithName(GetText("height_weight")).WithValue(GetText("height_weight_val", p.HeightM, p.WeightKg)).WithIsInline(true))
                            //.AddField(efb => efb.WithName(GetText("abilities")).WithValue(string.Join(",\n", p.Abilities.Select(a => a.Value))).WithIsInline(true)));
            return;
        }

        [NadekoCommand, Usage, Description, Alias]
        [Summary("Show the moves of the active pokemon")]
        public async Task ML()
        {
            var active = Context.User.ActivePokemon();
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithThumbnailUrl(active.IsShiny ? active.GetSpecies().Sprites.FrontShiny : active.GetSpecies().Sprites.Front)
                .AddField(efb => efb.WithName($"**{active.NickName.ToTitleCase()}'s moves**:").WithValue(active.PokemonMoves().Result).WithIsInline(true))); 
        }

        [NadekoCommand, Usage, Description, Alias]
        public async Task BattleTest()
        {
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithThumbnailUrl(_service.pokemonClasses[6].Sprites.FrontShiny)
                .WithDescription("Charmander Attacked Squirtle with Imagination!")
                .WithImageUrl(_service.pokemonClasses[197].Sprites.BackShiny));
                
        }
        [NadekoCommand, Usage, Description, Alias("catch")]
        [RequireContext(ContextType.Guild)]
        [Summary("replaces your selected pokemon with a wild")]
        public async Task CatchPkm(IGuildUser target, string pokemon)
        {
            var pkmList = Context.User.GetPokemon();
            var pkm = pkmList.Where(x => x.NickName == pokemon).DefaultIfEmpty(null).FirstOrDefault();
            if (pkm == null)
            {
                await ReplyAsync($"You dont have a pokemon named {pokemon}!");
                return;
            }

            var pokemonNumber = pkmList.IndexOf(pkm)+1;
            await CatchPkm(target, pokemonNumber);
        }

        [NadekoCommand, Usage, Description, Alias("catch")]
        [RequireContext(ContextType.Guild)]
        [Summary("replaces your selected pokemon with a wild")]
        public async Task CatchPkm(IUser target, int slot)
        {
            int shakeDelay = 500;
           
            if (!target.IsBot)
            {
                var embed = new EmbedBuilder().WithColor(Color.Purple)
                    .WithDescription("That's not a wild pokemon!")
                    .WithImageUrl(_service.GetRandomTrainerImage()).Build();
                await ReplyAsync("", false, embed);
                return;
            }
            Task delayTask;
            var msg = await ReplyAsync("<:pokeshake:439680842525310987>");
            delayTask = Task.Delay(shakeDelay * 3);
            if (!_cs.RemoveAsync(Context.User.Id, "Dropped a ball", 1).Result)
            {
                await ReplyAsync($"Not enough {_bc.BotConfig.CurrencySign}!");
                return;
            }

            await delayTask;
            await msg.ModifyAsync(x => x.Content = "<:pokeshake:439680842525310987>");
            await msg.ModifyAsync(x => x.Content = "<a:pokeshake:439674400933937152>");
            delayTask = Task.Delay(shakeDelay*6); 

            var targetPkm = target.ActivePokemon();
            var replacedPkm = Context.User.GetPokemon()[slot - 1];

            int ballchanceN =  rng.Next(0, 255);
            int catchRate = 195;
            await delayTask;
            await msg.ModifyAsync(x => x.Content = "<:pokeshake:439680842525310987>");
            await msg.ModifyAsync(x => x.Content = "<a:pokeshake:439674400933937152>");
            delayTask = Task.Delay(shakeDelay * 6);
            await delayTask;
            if (ballchanceN > catchRate)
            {
                
                await msg.ModifyAsync(x => x.Content = Context.User.Mention + "The Pokemon broke free!");
                return;
            }
            int M = rng.Next(0, 255);
            var catchChance = Math.Round((decimal)((targetPkm.MaxHP * 255 * 4) / (targetPkm.HP * 12)));
            await delayTask;
            await msg.ModifyAsync(x => x.Content = "<:pokeshake:439680842525310987>");
            await msg.ModifyAsync(x => x.Content = "<a:pokeshake:439674400933937152>");
            await Task.Delay(shakeDelay * 6);


            if (catchChance < M)
            {
                await msg.ModifyAsync(x => x.Content = Context.User.Mention + "The Pokemon broke free!");
                return;
            }
            targetPkm.Delete();
            targetPkm.Id = replacedPkm.Id;
            targetPkm.OwnerId = replacedPkm.OwnerId;
            targetPkm.IsActive = replacedPkm.IsActive;
            targetPkm.Update();
            var uow = _db.UnitOfWork;
            uow.PokemonSprite.Add(PokemonFunctions.GeneratePokemon(target));
            await uow.CompleteAsync();

            
            await msg.ModifyAsync(x => x.Content = $"**{replacedPkm.NickName}** released!\n Caught **{targetPkm.NickName}**! ✨ <:pokeshake:439680842525310987> ✨");
        }

        [NadekoCommand, Usage, Description, Alias("am")]
        [Summary("Show the moves of all your pokemon")]
        public async Task AllMoves()
        {
            var pokemon = Context.User.GetPokemon();
            string output = "";
            foreach (var pkm in pokemon)
            {
                output += $"**{pkm.NickName}'s moves**:\n{pkm.PokemonMoves().Result}\n\n";
            }
            await Context.User.SendMessageAsync(output);
            if (Context.Channel.GetType() != typeof(SocketDMChannel))//best way to determin dm?!?
                await ReplyAsync(Context.User.Mention + " I sent you a list of all your pokemon and their moves.");

        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Heals the specified users active pokemon (default self)")]
        public async Task Heal(IUser target = null)
        {
            target = target ?? Context.User;
            var pkm = target.ActivePokemon();
            if (pkm.HP == pkm.MaxHP)
            {
                await ReplyAsync($"{ pkm.NickName} is already at full health!");
                return;
            }
            if (_cs.RemoveAsync(Context.User.Id, "Healed a pokemon", 1).Result)
            {
                pkm.Heal();
                await ReplyAsync($"**{target.ActivePokemon().NickName}** has been healed for 1 {_bc.BotConfig.CurrencySign}!");
            }
            else
                await ReplyAsync("You need 1 point to heal");
        }
        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Heals all pokemon of the specified user (default self)")]
        public async Task Healall(IUser target = null)
        {
             target= target ?? Context.User;
            var toheal = target.GetPokemon().Where(x => x.HP < x.MaxHP);
            var count = toheal.Count();
            if (_cs.RemoveAsync(Context.User.Id, "Healed all pokemon", count).Result)
            {
                foreach (var pkm in toheal)
                    pkm.Heal();
                await ReplyAsync(count + " Pokemon healed for " + count + _bc.BotConfig.CurrencySign + "!");
            }
            else
                await NurseJoy();
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Show the moves of the active pokemon")]
        public async Task Heal(string target = null)
        {
            var pkm = Context.User.GetPokemon().Where(x => x.NickName == target).DefaultIfEmpty(null).FirstOrDefault();
            if (pkm == null)
            {
                await ReplyAsync($"{Context.User.Mention} You dont have a pokemon named **{target}**!");
                return;
            }
            if (pkm.HP == pkm.MaxHP)
            {
                await ReplyAsync($"{ pkm.NickName} is already at full health!");
                return;
            }
            if (_cs.RemoveAsync(Context.User.Id, "Healed a pokemon", 1).Result)
            {
                pkm.Heal();
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
           
            var toheal = target.GetPokemon().Where(x => x.HP == 0);
            if (toheal.Count() == 6)
            {
                if (currency >= 6)
                {
                    var embedtxt = new EmbedBuilder().WithColor(Color.Magenta)
                   .WithDescription($"You have enough {_bc.BotConfig.CurrencyName} {_bc.BotConfig.CurrencySign} to heal yourself. Use `.healall`")
                   .WithThumbnailUrl(_service.GetRandomNurseImage()).Build();
                    await ReplyAsync("", false, embedtxt);
                    return;
                }
                foreach (var pkm in toheal)
                    pkm.Heal();
                
                var embed = new EmbedBuilder().WithColor(Color.Magenta)
                   .WithDescription(Context.User.Mention + ",\n Your Pokémon are fighting fit!\nWe hope to see you again!")
                   .WithThumbnailUrl(_service.GetRandomNurseImage()).Build();
                await ReplyAsync("", false, embed);
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(Color.Red)
                   .WithDescription(Context.User.Mention + ", you still have pokemon willing to fight! Get back in there!")
                   .WithThumbnailUrl(_service.GetRandomNurseImage()).Build();
                await ReplyAsync("", false, embed);
            }

        }


        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("Switches the active pokemon")]
        public async Task Switch(string name, string move = null)
        {
            var list = Context.User.GetPokemon();
            var newpkm = list.Where(x => x.NickName.ToLowerInvariant() == name.ToLowerInvariant().Trim()).DefaultIfEmpty(null).FirstOrDefault() ?? new PokemonSprite();
            var trainer = ((IGuildUser)Context.User).GetTrainerStats();
            if (trainer.MovesMade > 0)
            {
                await ReplyAsync("You can't do that right now.");
                return;
            }

            if (newpkm.NickName == null)
            {
                await ReplyAsync(Context.User.Mention + $", you dont have a pokemon named {name}!");
                return;
            }
            switch (PokemonFunctions.SwitchPokemon(Context.User, newpkm)) { 
                case PokemonFunctions.SwitchResult.TargetFainted:
                    await ReplyAsync(Context.User.Mention + ", " + newpkm.NickName + " has already fainted!");
                    return;
                case PokemonFunctions.SwitchResult.Pass:
                    trainer.MovesMade++;
                    Context.User.UpdateTrainerStats(trainer);
                    await ReplyAsync($"{Context.User.Mention} switched to **{newpkm.NickName}**");
                    break;
                case PokemonFunctions.SwitchResult.Failed:
                    await ReplyAsync("Something went wrong!");
                    return;
            }
            if (move != null)
            {
                if (Context.User.GetTrainerStats().LastAttackedBy == null)
                {
                    await ReplyAsync("Can't attack. Use `.attack @target move`");
                    return;
                }
                await Attack(move).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("learn a move from levelling")]
        public async Task Learn(string move)
        {
            var pkm = Context.User.ActivePokemon();
            var moves = await pkm.GetMoves();
            var repMove = _service.pokemonMoves[move];
            var intMove = moves.IndexOf(repMove) + 1;
            await Learn(intMove);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("learn a move from levelling")]
        public async Task Learn(int move = 0)
        {
            var pkm = Context.User.ActivePokemon();
            var moves = pkm.GetMoves();
            var learnMove = pkm.GetLearnableMove();
            if (learnMove == null)
            {
                await ReplyAsync($"**{pkm.NickName}** is not trying to learn any moves!");
                return;
            }
            if (move == 0)
            {
                await ReplyAsync($"**{pkm.NickName}** is trying to learn **{learnMove.Name}**!");
                return;
            }
            if ((await moves).Contains(_service.pokemonMoves[learnMove.Name]))
            {
                await ReplyAsync($"**{pkm.NickName}** already knows **{learnMove.Name}**!");
                return;
            }
            var oldMove = (string)typeof(PokemonSprite).GetProperty("Move" + move).GetValue(pkm);
            typeof(PokemonSprite).GetProperty("Move" + move).SetValue(pkm, learnMove.Name);
            PokemonFunctions.UpdatePokemon(pkm);
            await ReplyAsync($"**{pkm.NickName}** has forgotten how to use **{oldMove}**\n and has learned **{learnMove.Name}**!");
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public async Task Attack([Remainder] string moveString)
        {
            if (!Context.User.GetTrainerStats().LastAttackedBy.TryGetValue(Context.Guild.Id, out IUser user))
            {
                await ReplyAsync("Target a user with `.attack @user move`");
                return;
            }

            await DoAttack(Context.User, user, moveString);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public Task Attack([Summary("The User to target")] IUser target, [Remainder] string moveString) =>
            DoAttack(Context.User, target, moveString);
        

        [NadekoCommand, Usage, Description, Alias]
        [RequireContext(ContextType.Guild)]
        [Summary("attacks a target")]
        public Task Attack(string moveString, [Summary("The User to target")] IUser target) =>
            DoAttack(Context.User, target, moveString);

        

        public async Task DoAttack(IUser attacker, IUser target, [Remainder] string moveString)
        {
            var attackerPokemon = attacker.ActivePokemon();
            var moveList = attackerPokemon.GetMoves().Result.Where(x => x.Name == moveString.Trim());
            PokemonMove Move;
            if (moveList.Count() == 0)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription($"Cannot use \"{moveString}\", see `{Prefix}ML` for moves"));
                return;
            }
            else
                Move = moveList.First();
            var attackerStats = attacker.GetTrainerStats();
            var defenderStats = target.GetTrainerStats();
            if (attackerStats.MovesMade > TrainerStats.MaxMoves || attackerStats.LastAttacked.Contains(target.Id))
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription($"{attacker.Mention} already attacked {target.Mention}!"));
                return;
            }
            if (attackerPokemon.HP == 0)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription($"{attackerPokemon.NickName} has fainted and can't attack!"));
                return;
            }
            if (defenderStats.LastAttackedBy.ContainsKey(Context.Guild.Id))
                defenderStats.LastAttackedBy.Remove(Context.Guild.Id);
            defenderStats.LastAttackedBy.Add(Context.Guild.Id, attacker);
            var defenderPokemon = target.ActivePokemon();

            if (defenderPokemon.HP == 0)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription($"{defenderPokemon.NickName} has already fainted!"));
                return;
            }

            PokemonAttack attack = new PokemonAttack(attackerPokemon, defenderPokemon, Move);
            var msg = attack.AttackString();

            var damageDone = attack.Damage;
            defenderPokemon.HP -= damageDone;
            msg += $"{defenderPokemon.NickName} has {defenderPokemon.HP} HP left!";
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(_service.TypeColors[Move.Type])
                .WithThumbnailUrl(defenderPokemon.IsShiny? 
                defenderPokemon.GetSpecies().Sprites.FrontShiny: 
                defenderPokemon.GetSpecies().Sprites.Front)
                .WithDescription(msg)
                .WithImageUrl(attackerPokemon.IsShiny ?
                attackerPokemon.GetSpecies().Sprites.BackShiny :
                attackerPokemon.GetSpecies().Sprites.Back));
            //Update stats, you shall
            attacker.UpdateTrainerStats(attackerStats.Attack(target, damageDone));
            target.UpdateTrainerStats(defenderStats.Reset());
            attackerPokemon.Update();
            defenderPokemon.Update();

            if (defenderPokemon.HP <= 0)
            {
                
                var str = $"{defenderPokemon.NickName} fainted!\n" + (!target.IsBot ? $"{attackerPokemon.NickName}'s owner {attacker.Mention} receives 1 {_bc.BotConfig.CurrencySign}\n": "");
                var lvl = attackerPokemon.Level;
                var reward = new RewardType();

                    reward = attackerPokemon.Reward(defenderPokemon);
                    str += $"{attackerPokemon.NickName} gained {reward.RewardValue} XP from the battle\n";
                if (attackerPokemon.Level > lvl) //levled up
                {
                    str += $"**{attackerPokemon.NickName}** leveled up!\n**{attackerPokemon.NickName}** is now level **{attackerPokemon.Level}**\n";
                    str += reward.EvolutionText;
                }
                attackerPokemon.Update();
                defenderPokemon.Update();
                var list = target.GetPokemon().Where(s => (s.HP > 0 && s != defenderPokemon));
                if (list.Any())
                {
                    var toSet = list.FirstOrDefault();

                    switch (PokemonFunctions.SwitchPokemon(target, toSet))
                    {
                        case PokemonFunctions.SwitchResult.Pass:
                            {
                                str += $"\n{target.Mention}'s active pokemon set to **{toSet.NickName}**";
                                break;
                            }
                        case PokemonFunctions.SwitchResult.Failed:
                        case PokemonFunctions.SwitchResult.TargetFainted:
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
                        var pkmlist = target.GetPokemon();
                        foreach (var pkm in pkmlist)
                            pkm.Heal();
                    }
                    //do something?
                }
                //UpdatePokemon(attackerPokemon);
                //UpdatePokemon(defenderPokemon);
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor().WithDescription(str));
                if (!target.IsBot)
                    await _cs.AddAsync(attacker.Id, "Victorious in pokemon", 1);
                
            }
            if (target.IsBot)
            {
                var cpuMoves = target.ActivePokemon().GetMoves().Result;
                await DoAttack(target, attacker, cpuMoves[new Random().Next(0,cpuMoves.Count()-1)].Name);
                
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
            var active = Context.User.ActivePokemon();
            var output = "**" + active.NickName + "** renamed to **";
            active.Rename(name);
            active.Update();
            
            await ReplyAsync(output + active.NickName + "**");

        }

        [NadekoCommand, Usage, Description, Alias]
        [Summary("Shows your current party")]
        public async Task List(IUser user)
        {
            var count = user.GetPokemon().Where(x => x.HP > 0).Count();
            await ReplyAsync($"{user.Username} has {count} pokemon left!");
        }
        [NadekoCommand, Usage, Description, Alias]
        [Summary("Shows your current party")]
        public async Task List()
        {
            var list = Context.User.GetPokemon();
            string str = $"{Context.User.Mention}'s pokemon are:\n";
            foreach (var pkm in list)
            {
                if (pkm.IsActive)
                {
                    str += $"__**{pkm.NickName}** : *{pkm.GetSpecies().Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}__\n";
                }
                else if (pkm.HP == 0)
                {
                    str += $"⚰️~~**{pkm.NickName}** : *{pkm.GetSpecies().Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}~~\n";
                }
                else
                {
                    str += $"**{pkm.NickName}** : *{pkm.GetSpecies().Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}\n";
                }

            }
            await ReplyAsync(str);
        }

        [NadekoCommand, Usage, Description, Alias]
        [RequireOwner]
        [Summary("Shows your current party")]
        public async Task Tm(int slot, string move, IUser user = null)
        {
            var target = user ?? Context.User;
            var pkm = target.ActivePokemon();
            PokemonMove newMove;
            try
            {
                newMove = _service.pokemonMoves[move];
            }
            catch (Exception)
            {
                await ReplyAsync("Move not found");
                return;
            }


            var oldMove = (string)typeof(PokemonSprite).GetProperty("Move" + slot).GetValue(pkm);
            typeof(PokemonSprite).GetProperty("Move" + slot).SetValue(pkm, newMove.Name);
            PokemonFunctions.UpdatePokemon(pkm);
            await ReplyAsync($"**{pkm.NickName}** has forgotten how to use **{oldMove}**\n and has learned **{newMove.Name}**!");
        }


            string swapPokemon(IGuildUser user, string OldPokemon, string NewPokemon, int Level = 5)
        {
            var oldpkm = _db.UnitOfWork.PokemonSprite.GetAll().Where(x => x.OwnerId==(long)user.Id && x.NickName==OldPokemon).First();
            var newspecies = _service.pokemonClasses.Where(x => x.Name == NewPokemon).DefaultIfEmpty(null).First();
            oldpkm.SpeciesId = newspecies.ID;
            oldpkm.NickName = newspecies.Name;
            oldpkm.Level = 0;
            oldpkm.XP = 0;
            while (oldpkm.Level <= Level-1)
            {
                oldpkm.LevelUp();
            }
            oldpkm.HP = oldpkm.MaxHP;
            oldpkm.Update();
            return "Probably worked. idk, theres no error checking, do `.list` to be sure";
        }

        

        

        
    }
}
