using System.Collections.Generic;

namespace TelegramVoteBot.ResultViewer.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string[] Urls { get; set; }

        public List<Vote> Votes { get; set; }
    }
}
