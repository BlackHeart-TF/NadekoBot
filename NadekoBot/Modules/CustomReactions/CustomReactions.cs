using Discord.Commands;
using Discord.Modules;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.CustomReactions
{
    internal class CustomReactionsModule : DiscordModule
    {
        public override string Prefix { get; } = "";

        private Random rng = new Random();

        private Dictionary<Regex, Func<CommandEventArgs, Match, string>> commandFuncs;

        public CustomReactionsModule()
        {
            commandFuncs = new Dictionary<Regex, Func<CommandEventArgs, Match, string>>
                 {
                    {new Regex(@"%rng:?(\d{0,9})-?(\d{0,9})%"), (e,m) => {
                        int start, end;
                        if (int.TryParse(m.Groups[1].Value, out start) && int.TryParse(m.Groups[2].Value, out end)) {
                            return rng.Next(start, end).ToString();
                        }
                        else return rng.Next().ToString();
                        } },
                    {new Regex("%mention%"), (e,m) => NadekoBot.BotMention },
                    {new Regex("%user%"), (e,m) => e.User.Mention },
                    {new Regex("%target%"), (e,m) => e.GetArg("args")?.Trim() ?? "" },
                 };
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
             {
                 cgb.AddCheck(PermissionChecker.Instance);

                 foreach (var command in NadekoBot.Config.CustomReactions)
                 {
                     var commandName = command.Key.Replace("%mention%", NadekoBot.BotMention);

                     var c = cgb.CreateCommand(commandName);
                     if (commandName.Contains(NadekoBot.BotMention))
                         c.Alias(commandName.Replace("<@", "<@!"));
                     c.Description($"Custom reaction.\n**Usage**:{command.Key}")
                         .Parameter("args", ParameterType.Unparsed)
                         .Do(async e =>
                          {
                              string str = command.Value[rng.Next(0, command.Value.Count())];
                              foreach (var key in commandFuncs.Keys)
                              {
                                  foreach (Match m in key.Matches(str))
                                  {
                                      str = str.Replace(m.Value, commandFuncs[key](e, m));
                                  }
                              }

                              await e.Channel.SendMessage(str).ConfigureAwait(false);
                          });
                 }
             });
        }
    }
}