using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TelegramVoteBot.ApiClient.Entities;

namespace TelegramVoteBot.ApiClient.Persistence
{
    public class BotDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var connectionString = config.GetConnectionString("Default");

            optionsBuilder.UseSqlite(connectionString);
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Vote> Votes { get; set; }
    }
}
