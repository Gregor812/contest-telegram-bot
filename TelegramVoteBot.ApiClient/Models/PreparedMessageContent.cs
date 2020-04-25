using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramVoteBot.ApiClient.Models
{
    public class PreparedMessageContent
    {
        public string ResponseText { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
    }
}
