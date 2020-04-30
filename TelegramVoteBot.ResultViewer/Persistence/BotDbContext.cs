using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using TelegramVoteBot.ApiClient.Entities;

namespace TelegramVoteBot.ApiClient.Persistence
{
    public class BotDbContext : DbContext
    {
        private readonly IConfiguration _config;

        public DbSet<Project> Projects { get; set; }
        public DbSet<Vote> Votes { get; set; }

        public BotDbContext()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _config.GetConnectionString("Default");
            optionsBuilder.UseSqlite(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>()
                .Property(p => p.Urls)
                .HasConversion(
                    urls => string.Join(',', urls),
                    urls => urls.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
