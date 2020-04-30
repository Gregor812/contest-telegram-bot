using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramVoteBot.ApiClient.Persistence;

namespace TelegramVoteBot.ApiClient
{
    class Program
    {
        static async Task Main(string[] args)
        {

            await using var db = new BotDbContext();

            var results = await db.Projects
                .Include(p => p.Votes)
                .OrderByDescending(p => p.Votes.Count)
                .ToListAsync();
            
            Console.OutputEncoding = Encoding.UTF8;

            for (int i = 0; i < results.Count; ++i)
            {
                Console.WriteLine($"{i + 1} место: {results[i].Name} автор {results[i].Author} ({results[i].Votes.Count} голосов)");
            }
        }
    }
}
