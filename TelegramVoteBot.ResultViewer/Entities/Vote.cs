using System;

namespace TelegramVoteBot.ResultViewer.Entities
{
    public class Vote
    {
        public int Id { get; set; }
        public int TelegramUserId { get; set; }
        public DateTime LastModified { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}
