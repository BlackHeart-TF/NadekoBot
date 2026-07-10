using System;
using System.Collections.Generic;
using NadekoBot.Services;
using NadekoBot.Common.Attributes;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Modules.PokeBattle.Services;
using System.Linq;
using NadekoBot.Modules.PokeBattle.Extentions;
using NadekoBot.Modules.PokeBattle.Common;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using NadekoBot.Common;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.PokeBattle;

public class PokeBattle : NadekoModule<PokemonService>
{
    private static DbService _db;
    private readonly ICurrencyService _cs;
    private readonly IConfigService _config;
    private readonly IImageCache _images;
    private NadekoRandom rng = new NadekoRandom();
    private readonly string _prefix;
    private readonly PokemonService _service;

    public PokeBattle(DbService db, ICurrencyService cs, BotConfigService confs, IImageCache images, PokemonService service)
    {
        _db = db;
        _cs = cs;
        _config = confs;
        _images = images;
        _prefix = confs.GetSetting("prefix");
        _service = service;
    }

    [Cmd]
    [Summary("Show Pokemon QuickHelp")]
    public async Task Phelp()
    {
        await Response().Embed(new EmbedBuilder().WithOkColor()
            .WithTitle("Pokemon Commands:")
            .WithDescription(@"
**.list** *Shows your current party*
**.ml** *Shows your active pokemon's moves*
**.allmoves** *DMs you a full list of your pokemon moves*
**.active** *Gives details on the active pokemon (.active @user)*
**.heal** *Heals a pokemon costs 1* " + _config.GetSetting("currency.sign") + @"
**.healall** *Heals your party. Costs 1" + _config.GetSetting("currency.sign") + @" per pokemon*
**.nursejoy** *Heals your party once they have all fainted (only if you are too broke to .healall)*
**.switch name** *Switches to the specified pokemon*
**.rename newName** *Renames your active pokemon to newName*
**.elite4** *Shows the top 4 players and their best pokemon*
**.rank** *Shows your pokemon ranking (.rank @user)*
**.catch @botName pokemonToReplace** *replaces the specified pokemon with one of the bots pokemon*
**.learn moveToReplace** *replaces the specified move with one your pokemon is trying to learn from levelling*")).SendAsync();
    }

    [Cmd]
    [Summary("Shows information on a move")]
    public async Task move(string move)
    {
        move = move.ToLowerInvariant();
        var r = new Regex("[th]m\\d{1,2}");
        PokemonMove moveInfo;
        if (r.Match(move).Success)
            moveInfo = _service.pokemonMoves.Where(x => x.TMName == move).First();
        else
            moveInfo = _service.pokemonMoves[move];
        var embed =new EmbedBuilder().WithTitle(moveInfo.Name.Replace('-',' ').ToTitleCase() + (moveInfo.TMName != null ? $" ({moveInfo.TMName.ToUpperInvariant()})" : "")).WithDescription(moveInfo.FlavorText.Replace('\n', ' '))
            .AddField(efb => efb.WithName("Stats").WithValue($"Power: {(moveInfo.Power == 0 ? "--" : moveInfo.Power.ToString())}\nAcc: {moveInfo.Accuracy}\nType: {moveInfo.Type.ToTitleCase()}/{moveInfo.DamageType.ToTitleCase()}")).WithColor(_service.TypeColors[moveInfo.Type]);

        //await Context.Channel.EmbedAsync(embed);
        await Response().Embed(embed).SendAsync();
    }

    [Cmd]
    [Summary("Shows the top ranking")]
    public Task Elite4()
        => SendLeaderboardAsync(null, 4, "🏆 Elite Four");

    [Cmd]
    [Summary("Shows the top ranking")]
    public Task Rank(IUser target = null)
        => SendLeaderboardAsync((target ?? Context.User).Id, 5, "📊 Pokemon Rankings");

    private async Task SendLeaderboardAsync(ulong? anchorUserId, int count, string title)
    {
        var trainers = await _service.GetLeaderboardAsync(anchorUserId, count);

        if (trainers.Count == 0)
        {
            await Response()
                    .Embed(CreateEmbed()
                            .WithTitle(title)
                            .WithDescription("No trainers on the board yet. Battle to claim a spot."))
                    .SendAsync();
            return;
        }

        var embed = CreateEmbed()
                    .WithTitle(title)
                    .WithOkColor();

        var highlight = anchorUserId is { } anchorId
            ? trainers.FirstOrDefault(t => t.ID == (long)anchorId) ?? trainers[0]
            : trainers[0];
        var highlightSprite = _service.GetSpriteUrl(highlight.TopPokemon);
        if (!string.IsNullOrWhiteSpace(highlightSprite))
            embed.WithThumbnailUrl(highlightSprite);

        foreach (var trainer in trainers)
        {
            var user = _service.GetUserByID(trainer.ID);
            var mention = user?.Mention ?? $"<@{trainer.ID}>";
            var ace = trainer.TopPokemon;
            var speciesName = _service.GetSpecies(ace)?.Name.ToTitleCase() ?? ace.NickName.ToTitleCase();
            var isAnchor = anchorUserId is { } id && trainer.ID == (long)id;
            var rankLabel = FormatRankLabel(trainer.Rank);

            embed.AddField(
                isAnchor ? $"{rankLabel} #{trainer.Rank} ⬅" : $"{rankLabel} #{trainer.Rank}",
                $"{mention}\n**{trainer.TotalExp:N0}** total XP\nAce: **{ace.NickName.ToTitleCase()}** ({speciesName}) Lv.{ace.Level}",
                inline: false);
        }

        embed.WithFooter(anchorUserId is null
            ? $"Use `{prefix}rank` to see your standing · `{prefix}active @user` for details"
            : $"Use `{prefix}elite4` for the top 4 · `{prefix}active @user` for details");

        await Response().Embed(embed).SendAsync();
    }

    private static string FormatRankLabel(int rank)
        => rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => "▫️",
        };

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Get the pokemon of someone|yourself")]
    public async Task Active(IUser target = null)
    {
        if (target == null)
            target = Context.User;
        var active = await _service.GetActivePokemonAsync(target);
        var species = _service.GetSpecies(active);

        string stats = $@"**Attack:** {active.Attack}
**Defense:** {active.Defense}
**SpecialAttack:** {active.SpecialAttack}
**SpecialDefense:** {active.SpecialDefense}
**Speed:** {active.Speed}";
#if DEBUG
        stats += $"\n**StatusTurns:** {active.StatusTurns}";
#endif
        
        string state = $@"
**Level:** {active.Level}
**HP:** {active.HP}/{active.MaxHP}
**XP:** {active.XP}/{active.XPRequired()}
**Type:** {species.GetTypeString()}
**Status:** {active.StatusEffect}";

        var imageUrl = active.IsShiny? species.Sprites.FrontShiny: species.Sprites.Front;

        await Response().Embed(new EmbedBuilder().WithOkColor()
                        .WithTitle((active.IsShiny ? "✨ " : "") + active.NickName.ToTitleCase() + (active.IsShiny ? " ✨" : ""))
                        .WithDescription($"**Species:** {species.Name.ToTitleCase()} **Owner:** {target.Mention}")
                        .AddField(efb => efb.WithName("**State**")
                                .WithValue(state).WithIsInline(true))
                        .AddField(efb => efb.WithName("**Stats**")
                                .WithValue(stats)
                                .WithIsInline(true))
                        .WithImageUrl(imageUrl))
                        .SendAsync();
        return;
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Show the moves of the active pokemon")]
    public async Task ML(string name = null)
    {
        PokemonSprite active;
        if (name == null)
            active = await _service.GetActivePokemonAsync(Context.User);
        else
            active = (await _service.PokemonListAsync(Context.User)).Where(x => x.NickName == name).First();
        await Response().Embed(new EmbedBuilder().WithOkColor()
            .WithThumbnailUrl(active.IsShiny ? _service.GetSpecies(active).Sprites.FrontShiny : _service.GetSpecies(active).Sprites.Front)
            .AddField(efb => efb.WithName($"**{active.NickName.ToTitleCase()}'s Moves**:").WithValue(active.PokemonMoves().Result).WithIsInline(true))).SendAsync(); 
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("replaces your selected pokemon with a wild")]
    public async Task CatchPkm(IGuildUser target, string pokemon)
    {
        var pkmList = await _service.PokemonListAsync(Context.User);
        var pkm = pkmList.Where(x => x.NickName == pokemon).DefaultIfEmpty(null).FirstOrDefault();
        if (pkm == null)
        {
            await ReplyAsync($"You dont have a pokemon named {pokemon}!");
            return;
        }

        var pokemonNumber = pkmList.IndexOf(pkm)+1;
        await CatchPkm(target, pokemonNumber);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("replaces your selected pokemon with a wild")]
    public async Task CatchPkm(IUser target, int slot)
    {
        int shakeDelay = 500;
        
        if (!target.IsBot)
        {
            var embed = new EmbedBuilder().WithColor(Color.Purple)
                .WithDescription("That's not a wild pokemon!")
                .WithImageUrl(await _images.GetPokeBattlePlayerCatchAsync()).Build();
            await ReplyAsync("", false, embed);
            return;
        }
        Task delayTask;
        var msg = await ReplyAsync("<:pokeshake:439680842525310987>");
        delayTask = Task.Delay(shakeDelay * 3);
        if (!_cs.RemoveAsync(Context.User.Id, 1, new("used","Dropped a ball")).Result)
        {
            await ReplyAsync($"Not enough {_config.GetSetting("currency.sign")}!");
            return;
        }

        await delayTask;
        await msg.ModifyAsync(x => x.Content = "<:pokeshake:439680842525310987>");
        await msg.ModifyAsync(x => x.Content = "<a:pokeshake:439674400933937152>");
        delayTask = Task.Delay(shakeDelay*6); 

        var targetPkm = await _service.GetActivePokemonAsync(target);
        var replacedPkm = (await _service.PokemonListAsync(Context.User)).OrderBy(x => x.Id).ToList()[slot - 1];

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
        _service.DeletePokemon(targetPkm);
        targetPkm.Id = replacedPkm.Id;
        targetPkm.OwnerId = replacedPkm.OwnerId;
        targetPkm.IsActive = replacedPkm.IsActive;
        _service.UpdatePokemon(targetPkm);
        var uow = _db.GetDbContext();
        uow.PokemonSprite.Add(_service.GeneratePokemon(target));
        await uow.SaveChangesAsync();

        
        await msg.ModifyAsync(x => x.Content = $"**{replacedPkm.NickName}** released!\n Caught **{targetPkm.NickName}**! ✨ <:pokeshake:439680842525310987> ✨");
    }

    [Cmd]
    [Summary("Show the moves of all your pokemon")]
    public async Task AllMoves()
    {
        var pokemon = (await _service.PokemonListAsync(Context.User)).Where(x => x.HP > 0);
        string output = "";
        foreach (var pkm in pokemon)
        {
            output += $"**{pkm.NickName}'s moves**:\n{pkm.PokemonMoves().Result}\n\n";
        }
        await Context.User.SendMessageAsync(output);
        if (Context.Channel.GetType() != typeof(SocketDMChannel))//best way to determin dm?!?
            await ReplyAsync(Context.User.Mention + " I sent you a list of all your pokemon and their moves.");

    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Heals the specified users active pokemon (default self)")]
    public async Task Heal(IUser target = null)
    {
        target = target ?? Context.User;
        var pkm = await _service.GetActivePokemonAsync(target);
        if (pkm.HP == pkm.MaxHP && pkm.StatusEffect == "none")
        {
            await ReplyAsync($"{ pkm.NickName} is already at full health!");
            return;
        }
        if (_cs.RemoveAsync(Context.User.Id, 1, new("used","Healed a pokemon")).Result)
        {
            pkm.Heal();
            await ReplyAsync($"**{pkm.NickName}** has been healed for 1 {_config.GetSetting("currency.sign")}!");
        }
        else
            await ReplyAsync("You need 1 point to heal");
    }
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Heals all pokemon of the specified user (default self)")]
    public async Task Healall(IUser target = null)
    {
            target= target ?? Context.User;
        var toheal = (await _service.PokemonListAsync(target)).Where(x => x.HP < x.MaxHP);
        var count = toheal.Count();
        if (_cs.RemoveAsync(Context.User.Id, count, new("used","Healed all pokemon")).Result)
        {
            foreach (var pkm in toheal)
                pkm.Heal();
            await ReplyAsync(count + " Pokemon healed for " + count + _config.GetSetting("currency.sign") + "!");
        }
        else
            await NurseJoy();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Heals your pokemon")]
    public async Task Heal(string target = null)
    {
        var pkm = (await _service.PokemonListAsync(Context.User)).Where(x => x.NickName == target).DefaultIfEmpty(null).FirstOrDefault();
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
        if (_cs.RemoveAsync(Context.User.Id, 1, new("used","Healed a pokemon")).Result)
        {
            pkm.Heal();
            await ReplyAsync($"**{pkm.NickName}** has been healed for 1 {_config.GetSetting("currency.sign")}!");
        }
        else
            await ReplyAsync("You need 1 point to heal");
    }
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Heals you when your party is knocked out")]
    public async Task NurseJoy()
    {
            var target = (IGuildUser) Context.User;

        var party = (await _service.PokemonListAsync(target)).Where(x => x.HP > 0);
        var count = party.Where(x => x.HP > 0).Count();
        var toheal = party.Where(x => x.HP == 0);
        if (count == 0)
        {

            foreach (var pkm in toheal)
                pkm.Heal();
            
            var embed = new EmbedBuilder().WithColor(Color.Magenta)
                .WithDescription(Context.User.Mention + ",\n Your Pokémon are fighting fit!\nWe hope to see you again!")
                .WithThumbnailUrl(await _images.GetPokeBattleNurseJoyAsync()).Build();
            await ReplyAsync("", false, embed);
        }
        else
        {
            var embed = new EmbedBuilder().WithColor(Color.Red)
                .WithDescription(Context.User.Mention + ", you still have pokemon willing to fight! Get back in there! or use `.healall`")
                .WithThumbnailUrl(await _images.GetPokeBattleNurseJoyAsync()).Build();
            await ReplyAsync("", false, embed);
        }

    }


    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Switches the active pokemon")]
    public async Task Switch(string name, string move = null)
    {
        var list = (await _service.PokemonListAsync(Context.User)).Where(x => x.HP > 0);
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
        switch (await _service.SwitchPokemonAsync(Context.User, newpkm)) { 
            case SwitchResult.TargetFainted:
                await ReplyAsync(Context.User.Mention + ", " + newpkm.NickName + " has already fainted!");
                return;
            case SwitchResult.Pass:
                trainer.MovesMade++;
                Context.User.UpdateTrainerStats(trainer);
                await ReplyAsync($"{Context.User.Mention} switched to **{newpkm.NickName}**");
                break;
            case SwitchResult.Failed:
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("learn a move from levelling")]
    public async Task Learn(string move)
    {
        var pkm = await _service.GetActivePokemonAsync(Context.User);
        var moves = _service.GetMoves(pkm);
        var repMove = _service.pokemonMoves[move];
        var intMove = moves.IndexOf(repMove) + 1;
        await Learn(intMove);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("learn a move from levelling")]
    public async Task Learn(int move = 0)
    {
        var pkm = await _service.GetActivePokemonAsync(Context.User);
        var moves = _service.GetMoves(pkm);
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
        if (moves.Contains(_service.pokemonMoves[learnMove.Name]))
        {
            await ReplyAsync($"**{pkm.NickName}** already knows **{learnMove.Name}**!");
            return;
        }
        var oldMove = (string)typeof(PokemonSprite).GetProperty("Move" + move).GetValue(pkm);
        typeof(PokemonSprite).GetProperty("Move" + move).SetValue(pkm, learnMove.Name);
        _service.UpdatePokemon(pkm);
        await ReplyAsync($"**{pkm.NickName}** has forgotten how to use **{oldMove}**\n and has learned **{learnMove.Name}**!");
    }

    [Cmd]
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("attacks a target")]
    public Task Attack([Summary("The User to target")] IUser target, [Remainder] string moveString) =>
        DoAttack(Context.User, target, moveString);
    

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("attacks a target")]
    public Task Attack(string moveString, [Summary("The User to target")] IUser target) =>
        DoAttack(Context.User, target, moveString);

    

    public async Task DoAttack(IUser attacker, IUser target, [Remainder] string moveString)
    {
        if (attacker == target)
        {
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription($"Stop being a masochist.")).SendAsync();
            return;
        }
        var moveStringLower = moveString.ToLowerInvariant().Trim();
        var attackerPokemon = await _service.GetActivePokemonAsync(attacker);
        var move = _service.GetMoveAsync(attackerPokemon,moveStringLower);

        if (move == null)
        {
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription($"Cannot use \"{moveString}\", see `{_prefix}ML` for moves")).SendAsync();
            return;
        }

        var attackerStats = attacker.GetTrainerStats();
        var defenderStats = target.GetTrainerStats();
        if (attackerStats.MovesMade > TrainerStats.MaxMoves || attackerStats.LastAttacked.Contains(target.Id))
        {
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription($"{attacker.Mention} already attacked {target.Mention}!")).SendAsync();
            return;
        }
        if (attackerPokemon.HP == 0)
        {
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription($"{attackerPokemon.NickName} has fainted and can't attack!")).SendAsync();
            return;
        }
        if (defenderStats.LastAttackedBy.ContainsKey(Context.Guild.Id))
            defenderStats.LastAttackedBy.Remove(Context.Guild.Id);
        defenderStats.LastAttackedBy.Add(Context.Guild.Id, attacker);
        var defenderPokemon = await _service.GetActivePokemonAsync(target);

        if (defenderPokemon.HP == 0)
        {
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription($"{defenderPokemon.NickName} has already fainted!")).SendAsync();
            return;
        }

        PokemonAttack attack = new PokemonAttack(attackerPokemon, defenderPokemon, move);
        attack.Commit();
        var msg = attack.AttackString();

        var defenderSpecies = _service.GetSpecies(defenderPokemon);
        var attackerSpecies = _service.GetSpecies(attackerPokemon);
        await Response().Embed(new EmbedBuilder().WithColor(_service.TypeColors[move.Type])
            .WithThumbnailUrl(defenderPokemon.IsShiny? defenderSpecies.Sprites.FrontShiny: defenderSpecies.Sprites.Front)
            .WithDescription(msg)
            .WithImageUrl(attackerPokemon.IsShiny ? attackerSpecies.Sprites.BackShiny : attackerSpecies.Sprites.Back)).SendAsync();
        //Update stats, you shall
        attacker.UpdateTrainerStats(await attackerStats.AttackAsync(target, attack.Damage));
        target.UpdateTrainerStats(defenderStats.Reset());

        if (defenderPokemon.HP <= 0)
        {
            
            var str = $"{defenderPokemon.NickName} fainted!\n" + (!target.IsBot ? $"{attackerPokemon.NickName}'s owner {attacker.Mention} receives 1 {_config.GetSetting("currency.sign")}\n": "");
            var lvl = attackerPokemon.Level;
            var reward = new RewardType();

                reward = attackerPokemon.Reward(defenderPokemon);
                str += $"{attackerPokemon.NickName} gained {reward.RewardValue} XP from the battle\n";
            if (attackerPokemon.Level > lvl) //levled up
            {
                str += $"**{attackerPokemon.NickName}** leveled up!\n**{attackerPokemon.NickName}** is now level **{attackerPokemon.Level}**\n";
                str += reward.EvolutionText;
            }
            _service.UpdatePokemon(attackerPokemon);
            _service.UpdatePokemon(defenderPokemon);
            var list = (await _service.PokemonListAsync(target)).Where(s => (s.HP > 0 && s != defenderPokemon));
            if (list.Any())
            {
                var toSet = list.FirstOrDefault();

                switch (await _service.SwitchPokemonAsync(target, toSet))
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
                    _service.DeletePokemon(defenderPokemon);
                }
                //do something?
            }
            await Response().Embed(new EmbedBuilder().WithErrorColor().WithDescription(str)).SendAsync();
            if (!target.IsBot)
                await _cs.AddAsync(attacker.Id, 1, new("Reward",$"Defeated {target.Username}'s pokemon {defenderPokemon.NickName}!"));
            
        }
        if (target.IsBot)
        {
            var cpuMoves = _service.GetMoves(await _service.GetActivePokemonAsync(target));
            await DoAttack(target, attacker, cpuMoves[new Random().Next(0,cpuMoves.Count()-1)].Name);
            var botpkm = await _service.GetActivePokemonAsync(target);
            if (botpkm.HP <= 0)
                _service.DeletePokemon(botpkm);
        }
    }
    

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Show the moves of the active pokemon")]
    public async Task Rename([Remainder] string name)
    {
        var active = await _service.GetActivePokemonAsync(Context.User);
        var output = "**" + active.NickName + "** renamed to **";
        active.Rename(name);
        _service.UpdatePokemon(active);
        
        await ReplyAsync(output + active.NickName + "**");

    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Summary("Shows your current party")]
    public async Task List(IUser user = null)
    {
        user = user ?? Context.User;
        if (!(user.IsBot || user.Id == Context.Message.Author.Id))
        {
            var count = (await _service.PokemonListAsync(user)).Where(x => x.HP > 0).Count();
            await ReplyAsync($"{user.Username} has {count} pokemon left!");
        }
        else
        {
            var list = await _service.PokemonListAsync(user);
            string str = $"{user.Mention}'s pokemon are:\n";
            foreach (var pkm in list)
            {
                var species = _service.GetSpecies(pkm);
                if (pkm.IsActive)
                {
                    str += $"{(pkm.HP==0 ? "⚰️":"")}__**{pkm.NickName}** : *{species.Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}__\n";
                }
                else if (pkm.HP == 0)
                {
                    str += $"⚰️~~**{pkm.NickName}** : *{species.Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}~~\n";
                }
                else
                {
                    str += $"**{pkm.NickName}** : *{species.Name}* Level: {pkm.Level} HP: {pkm.HP}/{pkm.MaxHP}\n";
                }

            }
            await ReplyAsync(str);
        }
    }
    

    [RequireOwner]
    [Summary("Replaces a users active pokemon's move (admin only)")]
    public async Task Tm(int slot, string move, IUser user = null)
    {
        var target = user ?? Context.User;
        var pkm = await _service.GetActivePokemonAsync(target);
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

        string? oldMove = null;
        switch (slot)
        {
            case 1:
                oldMove = pkm.Move1;
                pkm.Move1 = newMove.Name;
                break;
            case 2:
                oldMove = pkm.Move2;
                pkm.Move2 = newMove.Name;
                break;
            case 3:
                oldMove = pkm.Move3;
                pkm.Move3 = newMove.Name;
                break;
            case 4:
                oldMove = pkm.Move4;
                pkm.Move4 = newMove.Name;
                break;
        }
        _service.UpdatePokemon(pkm);
        await ReplyAsync($"**{pkm.NickName}** has forgotten how to use **{oldMove}**\n and has learned **{newMove.Name}**!");
    }

}
