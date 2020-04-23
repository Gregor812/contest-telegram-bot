using System.Collections.Generic;

namespace TelegramVoteBot.ApiClient.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }

        public List<Vote> Votes { get; set; }
    }
}
