using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
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
                        switch (update.Type)
                        {
                            case UpdateType.Message:
                                await HandleNewMessageAsync(update.Message, responses, workingChatId, cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            case UpdateType.CallbackQuery:
                                await HandleNewCallbackQueryAsync(update.CallbackQuery, responses, cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            default:
                                break;
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
                        if (response.UpdateMessage)
                        {
                            await botClient.EditMessageReplyMarkupAsync(response.ChatId,
                                response.UpdatingMessageId, response.InlineKeyboardMarkup,
                                cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(response.ChatId, response.Message,
                                    replyToMessageId: response.ReplyToMessageId,
                                    replyMarkup: response.InlineKeyboardMarkup,
                                    disableWebPagePreview: true,
                                    cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
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

        private async Task HandleNewMessageAsync(Message message,
            ConcurrentQueue<Response> responses, long? workingChatId,
            CancellationToken cancellationToken)
        {
            string responseText;
            InlineKeyboardMarkup inlineKeyboardMarkup = null;

            if (workingChatId != null && message.Chat.Id != workingChatId)
            {
                responseText = "Вронг чят айди";
            }
            else
            {
                var isMessageContainingCommands = message.Entities?
                    .Where(e => e.Type == MessageEntityType.BotCommand)
                    .Any();

                if (isMessageContainingCommands is null || isMessageContainingCommands == false)
                    return;

                var messageTokens = message.Text?
                    .Replace("@nordic_vote_bot", string.Empty)
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if (messageTokens is null || !messageTokens.Any())
                    return;

                switch (messageTokens[0])
                {
                    case "/list":
                        responseText = "Нажмите на кнопку для голосования за понравившийся проект";
                        var projects = await _db.Projects
                            .Include(p => p.Votes)
                            .OrderBy(p => p.Id)
                            .ToListAsync(cancellationToken);

                        if (!projects.Any())
                        {
                            inlineKeyboardMarkup = InlineKeyboardMarkup.Empty();
                            break;
                        }

                        var sb = new StringBuilder();
                        var buttons = new InlineKeyboardButton[projects.Count][];

                        for (int i = 0; i < projects.Count; i++)
                        {
                            var currentProject = projects[i];
                            buttons[i] = new InlineKeyboardButton[1];
                            var votesCount = currentProject.Votes.Count;
                            string votesText;

                            if (votesCount % 10 == 0)
                            {
                                votesText = $"{votesCount} голосов";
                            }
                            else if (votesCount % 10 == 1 && votesCount % 100 != 11)
                            {
                                votesText = $"{votesCount} голос";
                            }
                            else if (votesCount % 10 > 1 && votesCount % 10 < 5)
                            {
                                if (votesCount % 100 > 11 && votesCount % 100 < 15)
                                    votesText = $"{votesCount} голосов";
                                else
                                    votesText = $"{votesCount} голоса";
                            }
                            else
                            {
                                votesText = $"{votesCount} голосов";
                            }

                            buttons[i][0] =
                                InlineKeyboardButton.WithCallbackData($"Проект №{currentProject.Id}: {votesText}", currentProject.Id.ToString());
                            sb.AppendLine($"Проект №{currentProject.Id}: {currentProject.Name}")
                                .AppendLine($"Автор {currentProject.Author}");

                            foreach (var url in currentProject.Urls)
                            {
                                sb.AppendLine(url);
                            }

                            sb.AppendLine();
                        }

                        responseText = sb.ToString();
                        inlineKeyboardMarkup = new InlineKeyboardMarkup(buttons);
                        break;

                    default:
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

            InlineKeyboardMarkup inlineKeyboardMarkup = null;
            var message = callbackQuery.Message;
            var projectId = int.Parse(callbackQuery.Data);
            var userId = callbackQuery.From.Id;

            var alreadyExistingVote = await _db.Votes
                .FirstOrDefaultAsync(v => v.TelegramUserId == userId, cancellationToken);

            if (alreadyExistingVote != null)
            {
                if (alreadyExistingVote.ProjectId == projectId)
                {
                    _db.Votes.Remove(alreadyExistingVote);
                }
                else
                {
                    alreadyExistingVote.ProjectId = projectId;
                }
            }
            else
            {
                await _db.Votes.AddAsync(new Vote
                {
                    ProjectId = projectId,
                    TelegramUserId = userId
                }, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            var updatedProjects = await projects.ToListAsync(cancellationToken);

            if (message is null)
                return;

            var sb = new StringBuilder();
            var buttons = new InlineKeyboardButton[updatedProjects.Count][];

            for (int i = 0; i < updatedProjects.Count; i++)
            {
                var currentProject = updatedProjects[i];
                buttons[i] = new InlineKeyboardButton[1];
                var votesCount = currentProject.Votes.Count;
                string votesText;

                if (votesCount % 10 == 0)
                {
                    votesText = $"{votesCount} голосов";
                }
                else if (votesCount % 10 == 1 && votesCount % 100 != 11)
                {
                    votesText = $"{votesCount} голос";
                }
                else if (votesCount % 10 > 1 && votesCount % 10 < 5)
                {
                    if (votesCount % 100 > 11 && votesCount % 100 < 15)
                        votesText = $"{votesCount} голосов";
                    else
                        votesText = $"{votesCount} голоса";
                }
                else
                {
                    votesText = $"{votesCount} голосов";
                }

                buttons[i][0] =
                    InlineKeyboardButton.WithCallbackData($"Проект №{currentProject.Id}: {votesText}",
                        currentProject.Id.ToString());
                sb.AppendLine($"Проект №{currentProject.Id}: {currentProject.Name}")
                    .AppendLine($"Автор {currentProject.Author}");

                foreach (var url in currentProject.Urls)
                {
                    sb.AppendLine(url);
                }

                sb.AppendLine();
            }


            var responseText = sb.ToString();
            inlineKeyboardMarkup = new InlineKeyboardMarkup(buttons);

            var response = new Response
            {
                ChatId = message.Chat.Id,
                Message = responseText,
                ReplyToMessageId = message.MessageId,
                InlineKeyboardMarkup = inlineKeyboardMarkup,
                UpdateMessage = true,
                UpdatingMessageId = message.MessageId
            };

            responses.Enqueue(response);
        }
    }
}
