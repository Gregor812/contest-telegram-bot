using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramVoteBot.ApiClient.Entities;
using TelegramVoteBot.ApiClient.Models;
using TelegramVoteBot.ApiClient.Persistence;

namespace TelegramVoteBot.ApiClient
{
    public class Bot
    {
        private readonly ConcurrentQueue<Update> _updates =
            new ConcurrentQueue<Update>();
        private readonly ConcurrentQueue<Response> _responses
            = new ConcurrentQueue<Response>();
        private readonly BotDbContext _db;
        private readonly BotConfig _botConfig;

        public Bot(BotConfig botConfig, BotDbContext db)
        {
            _botConfig = botConfig ??
                throw new ArgumentNullException(nameof(botConfig));
            _db = db ?? 
                throw new ArgumentNullException(nameof(db));
        }

        public async Task StartBotAsync(CancellationToken cancellationToken)
        {
            var botClient = new TelegramBotClient(_botConfig.Token);

            var gettingUpdatesTask = StartGettingUpdatesAsync(botClient,
                _updates, cancellationToken);
            var handlingUpdatesTask = StartUpdatesHandlingAsync(_updates,
                _responses, _botConfig.WorkingChatId, cancellationToken);
            var handlingResponsesTask =StartResponsesHandlingAsync(botClient,
                _responses, cancellationToken);

            await Task.WhenAll(gettingUpdatesTask,
                    handlingUpdatesTask,
                    handlingResponsesTask)
                .ConfigureAwait(false);
        }

        private async Task StartGettingUpdatesAsync(
            TelegramBotClient botClient,
            ConcurrentQueue<Update> updates,
            CancellationToken cancellationToken)
        {
            int updatesOffset = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Getting bot updates...");

                    var newUpdates = await botClient
                        .GetUpdatesAsync(offset: updatesOffset, limit: 50,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {newUpdates.Length} new update(s)");

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
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {e}");
                    Console.ForegroundColor = color;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task StartUpdatesHandlingAsync(
            ConcurrentQueue<Update> updates,
            ConcurrentQueue<Response> responses,
            long? workingChatId,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (updates.IsEmpty)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1),
                                cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    while (updates.TryDequeue(out var update))
                    {
                        switch (update.Type)
                        {
                            case UpdateType.Message:
                                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Handling message update...");
                                await HandleNewMessageAsync(update.Message, responses,
                                        workingChatId, cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            case UpdateType.CallbackQuery:
                                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Handling callback query update...");
                                await HandleNewCallbackQueryAsync(update.CallbackQuery,
                                        responses, cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                        }
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {e}");
                    Console.WriteLine("Cannot handle an update");
                    Console.ForegroundColor = color;
                }
            }
        }

        private async Task StartResponsesHandlingAsync(
            TelegramBotClient botClient,
            ConcurrentQueue<Response> responses,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    while (responses.TryDequeue(out var response))
                    {
                        Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Handling response...");
                        if (response.UpdateMessage)
                        {
                            await botClient.EditMessageReplyMarkupAsync(response.ChatId,
                                    response.UpdatingMessageId,
                                    response.InlineKeyboardMarkup,
                                    cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(response.ChatId, response.Message,
                                    replyToMessageId: response.ReplyToMessageId,
                                    replyMarkup: response.InlineKeyboardMarkup,
                                    disableWebPagePreview: true,
                                    parseMode: response.ParseMode,
                                    cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {e}");
                    Console.ForegroundColor = color;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task HandleNewMessageAsync(Message message,
            ConcurrentQueue<Response> responses, long? workingChatId,
            CancellationToken cancellationToken)
        {
            var isMessageContainingCommands = message.Entities?
                .Where(e => e.Type == MessageEntityType.BotCommand)
                .Any();

            if (isMessageContainingCommands is null || isMessageContainingCommands == false)
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Message contains no command");
                return;
            }

            string responseText;
            InlineKeyboardMarkup inlineKeyboardMarkup = null;

            if (workingChatId != null && message.Chat.Id != workingChatId)
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Wrong chat id");
                responseText = "Вронг чят айди";
            }
            else
            {
                var messageTokens = message.Text?
                    .Replace("@nordic_vote_bot", string.Empty)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if (messageTokens is null || !messageTokens.Any())
                    return;

                switch (messageTokens[0])
                {
                    case "/list":
                        Console.WriteLine(
                            $"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: handling /list command triggered by {message.From.Username ?? message.From.FirstName} (id{message.From.Id})");

                        var projects = await _db.Projects
                            .Include(p => p.Votes)
                            .OrderBy(p => p.Id)
                            .ToListAsync(cancellationToken);

                        if (!projects.Any())
                        {
                            inlineKeyboardMarkup = InlineKeyboardMarkup.Empty();
                            responseText = string.Empty;
                            break;
                        }

                        var messageContent = PrepareMessageContent(projects);

                        responseText = messageContent.ResponseText;
                        inlineKeyboardMarkup = messageContent.InlineKeyboardMarkup;
                        break;

                    default:
                        Console.WriteLine(
                            $"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: unknow command {messageTokens[0]} triggered by {message.From.Username ?? message.From.FirstName} (id{message.From.Id})");

                        responseText = "Неизвестная команда";
                        inlineKeyboardMarkup = InlineKeyboardMarkup.Empty();
                        break;
                }
                await _db.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var response = new Response
            {
                ChatId = message.Chat.Id,
                Message = responseText,
                ReplyToMessageId = message.MessageId,
                InlineKeyboardMarkup = inlineKeyboardMarkup
            };
            responses.Enqueue(response);
        }

        private async Task HandleNewCallbackQueryAsync(CallbackQuery callbackQuery,
            ConcurrentQueue<Response> responses, CancellationToken cancellationToken)
        {
            var projects = _db.Projects
                .Include(p => p.Votes)
                .OrderBy(p => p.Id);

            if (!(await projects.AnyAsync(cancellationToken)))
                return;

            var message = callbackQuery.Message;
            var projectId = int.Parse(callbackQuery.Data);
            var userId = callbackQuery.From.Id;
            var username = callbackQuery.From.Username;
            var userFirstName = callbackQuery.From.FirstName;
            var userMention = username is null ? 
                $"[{userFirstName}](tg://user?id={userId})" :
                $"@{username}";

            string responseMessage;

            var alreadyExistingVote = await _db.Votes
                .FirstOrDefaultAsync(v => v.TelegramUserId == userId, cancellationToken);

            if (alreadyExistingVote != null)
            {
                if (alreadyExistingVote.ProjectId == projectId)
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{userId}) canceled vote");
                    responseMessage = $"{userMention} id{ callbackQuery.From.Id} отменил голос";
                    _db.Votes.Remove(alreadyExistingVote);
                }
                else
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{callbackQuery.From.Id}) revoted");
                    responseMessage = $"{userMention} id{ callbackQuery.From.Id} изменил свой голос";
                    alreadyExistingVote.ProjectId = projectId;
                }
            }
            else
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{callbackQuery.From.Id}) voted");
                responseMessage = $"{userMention} id{ callbackQuery.From.Id} проголосовал";
                await _db.Votes.AddAsync(new Vote
                {
                    ProjectId = projectId,
                    TelegramUserId = userId
                }, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (message is null)
                return;

            var response = new Response
            {
                ChatId = message.Chat.Id,
                Message = responseMessage,
                ParseMode = username is null ? ParseMode.MarkdownV2 : ParseMode.Default
            };

            responses.Enqueue(response);
        }

        private PreparedMessageContent PrepareMessageContent(List<Project> projects)
        {
            var sb = new StringBuilder();
            var buttons = new InlineKeyboardButton[projects.Count][];

            for (int i = 0; i < projects.Count; i++)
            {
                var currentProject = projects[i];
                buttons[i] = new InlineKeyboardButton[1];

                buttons[i][0] =
                    InlineKeyboardButton.WithCallbackData($"Проект №{currentProject.Id}",
                        currentProject.Id.ToString());
                sb.AppendLine($"Проект №{currentProject.Id}: {currentProject.Name}")
                    .AppendLine($"Автор {currentProject.Author}");

                foreach (var url in currentProject.Urls)
                {
                    sb.AppendLine(url);
                }

                sb.AppendLine();
            }

            var result = new PreparedMessageContent
            {
                ResponseText = sb.ToString(),
                InlineKeyboardMarkup = new InlineKeyboardMarkup(buttons)
            };

            return result;
        }
    }
}
