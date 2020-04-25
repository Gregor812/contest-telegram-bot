using Microsoft.EntityFrameworkCore.Migrations;

namespace TelegramVoteBot.ApiClient.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    Urls = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelegramUserId = table.Column<int>(nullable: false),
                    ProjectId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 1, "@Vit", "Инвертор 24 - 220 В 1500 ВА", "https://telegra.ph/Proekt-1-Invertor-24-220-V-1500-VA-01-23" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 2, "@dmitriy_shi", "PIndastrial shield — модуль питания и интерфейса RS-485 для Raspberry PI", "https://habr.com/ru/post/486258/,https://gitlab.com/dm_sh/pindastrial_shield" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 3, "@Dab0G", "IoT шлюз Ethernet-RS485 на базе STM32", "https://habr.com/ru/post/488408/,https://github.com/mysensors-rus/STM32_Ethernet-RS485_gate" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 4, "@EfektaSB", "Ардуино термометр & гигрометр с E-PAPER на nRF52832 — или о том, что забыли выпустить производители", "https://habr.com/ru/post/452532/,https://youtu.be/T66y83lF-xg,https://github.com/smartboxchannel/EFEKTA-EINK-TEMP-HUM-SENSOR-NRF52" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 5, "@EfektaSB", "Беспроводной датчик протечки воды на nRF52832, DIY проект", "https://habr.com/ru/post/460177/,https://youtu.be/xsoeffaGAG0,https://youtu.be/5jZt3NWf9GA,https://github.com/smartboxchannel/EFEKTA_WATER_LEAK_SENSOR" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 6, "@EfektaSB", "Мини датчик света и удара | nRF52840", "https://habr.com/ru/post/478960/,https://youtu.be/I2ywIxp-RsE,https://github.com/smartboxchannel/EFEKTA-LIS2DW12-MAX44009-E73C" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 7, "@outwaves", "Модульный синтезатор", "https://telegra.ph/testovoe-01-26,https://github.com/Outwaves/stm32/tree/master/sequencer" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 8, "@hextomato", "Контроллер детского электромотоцикла KEMC1804", "https://telegra.ph/Kontroller-detskogo-ehlektromotocikla-KEMC1804-03-15,https://yadi.sk/d/NESmi9MeB7s8mg" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 9, "@Escaper19", "СИСТЕМА АВТОМАТИЧЕСКОГО РЕГУЛИРОВАНИЯ ТЕМПЕРАТУРЫ ВОЗДУХА В ТЕПЛИЦЕ", "https://telegra.ph/SISTEMA-AVTOMATICHESKOGO-REGULIROVANIYA-TEMPERATURY-VOZDUHA-V-TEPLICE-03-29,https://github.com/Escaper2/proect" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 10, "@Valentyn_Korniienko", "Подготовка к велосипедостроению.", "https://telegra.ph/Podgotovka-k-velosipedostroeniyu-04-14,https://github.com/ValentiWorkLearning/GradWork" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 11, "@tim_kh", "Все этапы создания робота для следования по линии, или как собрать все грабли с STM32", "https://telegra.ph/Vse-ehtapy-sozdaniya-robota-dlya-sledovaniya-po-linii-ili-kak-sobrat-vse-grabli-s-STM32-04-15" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 12, "@Dr_Zlo13", "Hardware User Interface Mockup Kit", "https://telegra.ph/Hardware-User-Interface-Mockup-Kit-04-15,https://github.com/DrZlo13/huimk" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 13, "@numenor", "Заметки о разработке МРРТ контроллера", "https://habr.com/ru/post/495548/,https://github.com/gardarica/mppt-2420-hardware" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Author", "Name", "Urls" },
                values: new object[] { 14, "@Velkarn", "Источник питания промышленного контроллера на 100 Вт", "https://telegra.ph/Istochnik-pitaniya-promyshlennogo-kontrollera-na-100-Vt-04-16" });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ProjectId",
                table: "Votes",
                column: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
