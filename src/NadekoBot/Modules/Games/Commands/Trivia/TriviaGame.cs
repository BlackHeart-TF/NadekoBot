﻿using Discord;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// todo rewrite?
// todo DB
namespace NadekoBot.Modules.Games.Commands.Trivia
{
    public class TriviaGame
    {
        private readonly SemaphoreSlim _guessLock = new SemaphoreSlim(1, 1);

        private IGuild guild { get; }
        private ITextChannel channel { get; }

        private int QuestionDurationMiliseconds { get; } = 30000;
        private int HintTimeoutMiliseconds { get; } = 6000;
        public bool ShowHints { get; set; } = true;
        private CancellationTokenSource triviaCancelSource { get; set; }

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> oldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; } = false;
        public bool ShouldStopGame { get; private set; }

        public int WinRequirement { get; } = 10;

        public TriviaGame(IGuild guild, ITextChannel channel, bool showHints, int winReq = 10)
        {
            ShowHints = showHints;
            this.guild = guild;
            this.channel = channel;
            WinRequirement = winReq;
            Task.Run(StartGame);
        }

        private async Task StartGame()
        {
            while (!ShouldStopGame)
            {
                // reset the cancellation source
                triviaCancelSource = new CancellationTokenSource();
                var token = triviaCancelSource.Token;
                // load question
                CurrentQuestion = TriviaQuestionPool.Instance.GetRandomQuestion(oldQuestions);
                if (CurrentQuestion == null)
                {
                    await channel.SendMessageAsync($":exclamation: Failed loading a trivia question").ConfigureAwait(false);
                    await End().ConfigureAwait(false);
                    return;
                }
                oldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again
                                                   //sendquestion
                await channel.SendMessageAsync($":question: **{CurrentQuestion.Question}**").ConfigureAwait(false);

                //receive messages
                NadekoBot.Client.MessageReceived += PotentialGuess;

                //allow people to guess
                GameActive = true;

                try
                {
                    //hint
                    await Task.Delay(HintTimeoutMiliseconds, token).ConfigureAwait(false);
                    if (ShowHints)
                        await channel.SendMessageAsync($":exclamation:**Hint:** {CurrentQuestion.GetHint()}").ConfigureAwait(false);

                    //timeout
                    await Task.Delay(QuestionDurationMiliseconds - HintTimeoutMiliseconds, token).ConfigureAwait(false);

                }
                catch (TaskCanceledException) { } //means someone guessed the answer
                GameActive = false;
                if (!triviaCancelSource.IsCancellationRequested)
                    await channel.SendMessageAsync($":clock2: :question: **Time's up!** The correct answer was **{CurrentQuestion.Answer}**").ConfigureAwait(false);
                NadekoBot.Client.MessageReceived -= PotentialGuess;
                // load next question if game is still running
                await Task.Delay(2000).ConfigureAwait(false);
            }
            await End().ConfigureAwait(false);
        }

        private async Task End()
        {
            ShouldStopGame = true;
            await channel.SendMessageAsync("**Trivia game ended**\n" + GetLeaderboard()).ConfigureAwait(false);
        }

        public async Task StopGame()
        {
            if (!ShouldStopGame)
                await channel.SendMessageAsync(":exclamation: Trivia will stop after this question.").ConfigureAwait(false);
            ShouldStopGame = true;
        }

        private async Task PotentialGuess(IMessage imsg)
        {
            try
            {
                if (!(imsg.Channel is IGuildChannel && imsg.Channel is ITextChannel)) return;
                if ((imsg.Channel as ITextChannel).Guild != guild) return;
                if (imsg.Author.Id == (await NadekoBot.Client.GetCurrentUserAsync()).Id) return;

                var guildUser = imsg.Author as IGuildUser;

                var guess = false;
                await _guessLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (GameActive && CurrentQuestion.IsAnswerCorrect(imsg.Content) && !triviaCancelSource.IsCancellationRequested)
                    {
                        Users.AddOrUpdate(guildUser, 0, (gu, old) => old++);
                        guess = true;
                    }
                }
                finally { _guessLock.Release(); }
                if (!guess) return;
                triviaCancelSource.Cancel();
                await channel.SendMessageAsync($"☑️ {guildUser.Mention} guessed it! The answer was: **{CurrentQuestion.Answer}**").ConfigureAwait(false);
                if (Users[guildUser] != WinRequirement) return;
                ShouldStopGame = true;
                await channel.SendMessageAsync($":exclamation: We have a winner! It's {guildUser.Mention}.").ConfigureAwait(false);
            }
            catch { }
        }

        public string GetLeaderboard()
        {
            if (Users.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.Append("**Leaderboard:**\n-----------\n");

            foreach (var kvp in Users.OrderBy(kvp => kvp.Value))
            {
                sb.AppendLine($"**{kvp.Key.Username}** has {kvp.Value} points".ToString().SnPl(kvp.Value));
            }

            return sb.ToString();
        }
    }
}
