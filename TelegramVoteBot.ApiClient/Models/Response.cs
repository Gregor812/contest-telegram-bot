using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramVoteBot.ApiClient.Models
{
    public class Response
    {
        public string Message { get; set; }
        public long ChatId { get; set; }
        public int ReplyToMessageId { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
        public bool UpdateMessage { get; set; }
        public int UpdatingMessageId { get; set; }
    }
}
