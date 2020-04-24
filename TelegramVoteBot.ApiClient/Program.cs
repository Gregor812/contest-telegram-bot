using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramVoteBot.ApiClient.Models;
using TelegramVoteBot.ApiClient.Persistence;

namespace TelegramVoteBot.ApiClient
{
    class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Press Ctrl+C to cancel");
            Console.CancelKeyPress += Console_CancelKeyPress;

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var botConfig = config.GetSection("Bot").Get<BotConfig>();

            await using var db = new BotDbContext();

            var bot = new Bot(botConfig, db);
            await bot.StartBotAsync(CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Operation has been cancelled by user");
            CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
            while (!CancellationTokenSource.IsCancellationRequested)
            { }
            Environment.Exit(-1);
        }
    }
}
