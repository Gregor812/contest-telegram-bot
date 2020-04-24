using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>()
                .Property(p => p.Urls)
                .HasConversion(
                    urls => string.Join(',', urls),
                    urls => urls.Split(',', StringSplitOptions.RemoveEmptyEntries));

            modelBuilder.Entity<Project>().HasData(
                new Project
                {
                    Id = 1,
                    Author = "@Vit",
                    Name = "Инвертор 24 - 220 В 1500 ВА",
                    Urls = new[] { "https://telegra.ph/Proekt-1-Invertor-24-220-V-1500-VA-01-23" }
                },
                new Project
                {
                    Id = 2,
                    Author = "@dmitriy_shi",
                    Name = "PIndastrial shield — модуль питания и интерфейса RS-485 для Raspberry PI",
                    Urls = new[] { "https://habr.com/ru/post/486258/", "https://gitlab.com/dm_sh/pindastrial_shield" }
                },
                new Project
                {
                    Id = 3,
                    Author = "@Dab0G",
                    Name = "IoT шлюз Ethernet-RS485 на базе STM32",
                    Urls = new[] { "https://habr.com/ru/post/488408/", "https://github.com/mysensors-rus/STM32_Ethernet-RS485_gate" }
                },
                new Project
                {
                    Id = 4,
                    Author = "@EfektaSB",
                    Name = "Ардуино термометр & гигрометр с E-PAPER на nRF52832 — или о том, что забыли выпустить производители",
                    Urls = new[] { "https://habr.com/ru/post/452532/", "https://youtu.be/T66y83lF-xg", "https://github.com/smartboxchannel/EFEKTA-EINK-TEMP-HUM-SENSOR-NRF52" }
                },
                new Project
                {
                    Id = 5,
                    Author = "@EfektaSB",
                    Name = "Беспроводной датчик протечки воды на nRF52832, DIY проект",
                    Urls = new[] { "https://habr.com/ru/post/460177/", "https://youtu.be/xsoeffaGAG0", "https://youtu.be/5jZt3NWf9GA", "https://github.com/smartboxchannel/EFEKTA_WATER_LEAK_SENSOR" }
                },
                new Project
                {
                    Id = 6,
                    Author = "@EfektaSB",
                    Name = "Мини датчик света и удара | nRF52840",
                    Urls = new[] { "https://habr.com/ru/post/478960/", "https://youtu.be/I2ywIxp-RsE", "https://github.com/smartboxchannel/EFEKTA-LIS2DW12-MAX44009-E73C" }
                },
                new Project
                {
                    Id = 7,
                    Author = "@outwaves",
                    Name = "Модульный синтезатор",
                    Urls = new[] { "https://telegra.ph/testovoe-01-26", "https://github.com/Outwaves/stm32/tree/master/sequencer" }
                },
                new Project
                {
                    Id = 8,
                    Author = "@hextomato",
                    Name = "Контроллер детского электромотоцикла KEMC1804",
                    Urls = new[] { "https://telegra.ph/Kontroller-detskogo-ehlektromotocikla-KEMC1804-03-15", "https://yadi.sk/d/NESmi9MeB7s8mg" }
                },
                new Project
                {
                    Id = 9,
                    Author = "@Escaper19",
                    Name = "СИСТЕМА АВТОМАТИЧЕСКОГО РЕГУЛИРОВАНИЯ ТЕМПЕРАТУРЫ ВОЗДУХА В ТЕПЛИЦЕ",
                    Urls = new[] { "https://telegra.ph/SISTEMA-AVTOMATICHESKOGO-REGULIROVANIYA-TEMPERATURY-VOZDUHA-V-TEPLICE-03-29", "https://github.com/Escaper2/proect" }
                },
                new Project
                {
                    Id = 10,
                    Author = "@Valentyn_Korniienko",
                    Name = "Подготовка к велосипедостроению.",
                    Urls = new[] { "https://telegra.ph/Podgotovka-k-velosipedostroeniyu-04-14", "https://github.com/ValentiWorkLearning/GradWork" }
                },
                new Project
                {
                    Id = 11,
                    Author = "@tim_kh",
                    Name = "Все этапы создания робота для следования по линии, или как собрать все грабли с STM32",
                    Urls = new[] { "https://telegra.ph/Vse-ehtapy-sozdaniya-robota-dlya-sledovaniya-po-linii-ili-kak-sobrat-vse-grabli-s-STM32-04-15" }
                },
                new Project
                {
                    Id = 12,
                    Author = "@Dr_Zlo13",
                    Name = "Hardware User Interface Mockup Kit",
                    Urls = new[] { "https://telegra.ph/Hardware-User-Interface-Mockup-Kit-04-15", "https://github.com/DrZlo13/huimk" }
                },
                new Project
                {
                    Id = 13,
                    Author = "@numenor",
                    Name = "Заметки о разработке МРРТ контроллера",
                    Urls = new[] { "https://habr.com/ru/post/495548/", "https://github.com/gardarica/mppt-2420-hardware" }
                },
                new Project
                {
                    Id = 14,
                    Author = "@Velkarn",
                    Name = "Источник питания промышленного контроллера на 100 Вт",
                    Urls = new[] { "https://telegra.ph/Istochnik-pitaniya-promyshlennogo-kontrollera-na-100-Vt-04-16" }
                });
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Vote> Votes { get; set; }
    }
}
