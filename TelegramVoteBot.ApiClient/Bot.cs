using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramVoteBot.ApiClient.Models;
using TelegramVoteBot.ApiClient.Persistence;

namespace TelegramVoteBot.ApiClient
{
    public class Bot
    {
        private readonly ConcurrentQueue<Update> _updates = new ConcurrentQueue<Update>();
        private readonly ConcurrentQueue<Response> _responses = new ConcurrentQueue<Response>();
        private readonly BotDbContext _db;
        private readonly BotConfig _botConfig;

        public Bot(BotConfig botConfig, BotDbContext db)
        {
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task StartBotAsync(CancellationToken cancellationToken)
        {
            var botClient = new TelegramBotClient(_botConfig.Token);

            var gettingUpdatesTask = StartGettingUpdatesAsync(botClient, _updates, cancellationToken);
            var handlingFailedUpdatesTask =
                StartUpdatesHandlingAsync(_updates, _responses, _botConfig.WorkingChatId, cancellationToken);
            var handlingResponsesTask =
                StartResponsesHandlingAsync(botClient, _responses, cancellationToken);

            await Task.WhenAll(gettingUpdatesTask, handlingFailedUpdatesTask, handlingResponsesTask)
                .ConfigureAwait(false);
        }

        private async Task StartGettingUpdatesAsync(TelegramBotClient botClient,
            ConcurrentQueue<Update> updates, CancellationToken cancellationToken)
        {
            int updatesOffset = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Console.WriteLine("Getting bot updates...");
                    var newUpdates = await botClient
                        .GetUpdatesAsync(offset: updatesOffset, limit: 50, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    Console.WriteLine($"{newUpdates.Length} new update(s)");
                    if (newUpdates.Any())
                    {
                        updatesOffset = newUpdates.Select(u => u.Id).Max() + 1;
                        foreach (var update in newUpdates)
                        {
                            updates.Enqueue(update);
                        }
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ForegroundColor = color;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task StartUpdatesHandlingAsync(ConcurrentQueue<Update> updates,
            ConcurrentQueue<Response> responses, long? workingChatId,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (updates.IsEmpty)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    while (updates.TryDequeue(out var update))
                    {
                        if (update.Type == UpdateType.Message)
                        {
                            await HandleNewUpdateAsync(update, responses, workingChatId, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.WriteLine("Cannot handle an update");
                    Console.ForegroundColor = color;
                }
            }
        }

        private async Task StartResponsesHandlingAsync(TelegramBotClient botClient,
            ConcurrentQueue<Response> responses, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    while (responses.TryDequeue(out var response))
                    {
                        await botClient.SendTextMessageAsync(response.ChatId, response.Message,
                            replyToMessageId: response.ReplyToMessageId, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ForegroundColor = color;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task HandleNewUpdateAsync(Update update,
            ConcurrentQueue<Response> responses, long? workingChatId,
            CancellationToken cancellationToken)
        {
            string responseText;
            if (update.Message is null)
                return;

            if (workingChatId != null && update.Message.Chat.Id != workingChatId)
            {
                responseText = "Вронг чят айди";
            }
            else
            {
                var isMessageContainingCommands = update.Message.Entities?
                    .Where(e => e.Type == MessageEntityType.BotCommand)
                    .Any();

                if (isMessageContainingCommands is null || isMessageContainingCommands == false)
                    return;

                var messageTokens = update.Message.Text?
                    .Replace("@nordic_vote_bot", string.Empty)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if (messageTokens is null || !messageTokens.Any())
                    return;

                switch (messageTokens[0])
                {

                    default:
                        responseText = "Неизвестная команда";
                        break;
                }
                await _db.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var response = new Response
            {
                ChatId = update.Message.Chat.Id,
                Message = responseText,
                ReplyToMessageId = update.Message.MessageId
            };
            responses.Enqueue(response);
        }
    }
}
