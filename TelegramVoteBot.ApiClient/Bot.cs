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
            var handlingUpdatesTask = StartUpdatesHandlingAsync(
                botClient, _updates, _responses,
                _botConfig.WorkingChatId, cancellationToken);
            var handlingResponsesTask = StartResponsesHandlingAsync(botClient,
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
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: Getting bot updates...");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var newUpdates = await botClient
                        .GetUpdatesAsync(offset: updatesOffset, limit: 50,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (newUpdates.Any())
                    {
                        Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {newUpdates.Length} new update(s)");

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
            TelegramBotClient botClient,
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
                                await HandleNewMessageAsync(botClient, update.Message, responses,
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

        private async Task HandleNewMessageAsync(
            TelegramBotClient botClient, Message message,
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
            var isUserWorkingChatMember = true;

            if (workingChatId != null)
            {
                if (workingChatId == message.Chat.Id)
                    return;

                isUserWorkingChatMember = await IsUserAWorkingChatMemberAsync(botClient, message.From,
                        workingChatId.Value, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!isUserWorkingChatMember)
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {message.From.Username ?? message.From.FirstName} (id{message.From.Id})");
                responseText = "Голос не учитывается, т.к. вы не являетесь участником чата Nordic Energy";
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

                        var messageContent = PrepareMessageContent(projects, message.From.Id);

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

        private async Task<bool> IsUserAWorkingChatMemberAsync(
            TelegramBotClient botClient,
            User user, long workingChatId,
            CancellationToken cancellationToken)
        {
            var chatMember = await botClient.GetChatMemberAsync(
                new ChatId(workingChatId), user.Id, cancellationToken);
            var status = chatMember.Status;
            return status == ChatMemberStatus.Creator ||
                   status == ChatMemberStatus.Administrator ||
                   status == ChatMemberStatus.Member;
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
            
            var alreadyExistingVote = await _db.Votes
                .FirstOrDefaultAsync(v => v.TelegramUserId == userId, cancellationToken);

            if (alreadyExistingVote != null)
            {
                if (alreadyExistingVote.ProjectId == projectId)
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{userId}) canceled vote");
                    _db.Votes.Remove(alreadyExistingVote);
                }
                else
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{callbackQuery.From.Id}) revoted");
                    alreadyExistingVote.ProjectId = projectId;
                    alreadyExistingVote.LastModified = DateTime.Now;
                }
            }
            else
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:s}: {username ?? userFirstName} (id{callbackQuery.From.Id}) voted");
                await _db.Votes.AddAsync(new Vote
                {
                    ProjectId = projectId,
                    TelegramUserId = userId,
                    LastModified = DateTime.Now
                }, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (message is null)
                return;

            var unsortedProjects = await _db.Projects
                .Include(p => p.Votes)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var content = PrepareMessageContent(unsortedProjects, userId);

            var response = new Response
            {
                ChatId = message.Chat.Id,
                Message = content.ResponseText,
                InlineKeyboardMarkup = content.InlineKeyboardMarkup,
                UpdatingMessageId = message.MessageId,
                UpdateMessage = true
            };

            responses.Enqueue(response);
        }

        private PreparedMessageContent PrepareMessageContent(List<Project> projects, long userId)
        {
            var sb = new StringBuilder();
            var buttons = new InlineKeyboardButton[projects.Count][];

            for (int i = 0; i < projects.Count; i++)
            {
                var currentProject = projects[i];
                buttons[i] = new InlineKeyboardButton[1];

                var text = currentProject.Votes.Any(v => v.TelegramUserId == userId)
                    ? $"Проект №{currentProject.Id} ✅"
                    : $"Проект №{currentProject.Id}";

                buttons[i][0] =
                    InlineKeyboardButton.WithCallbackData(text,
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
