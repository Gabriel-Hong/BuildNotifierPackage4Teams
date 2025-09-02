namespace BuildNotifierPackagesForTeams.Models
{
    public class TeamsMessage
    {
        public string Type { get; set; }
        public TeamsAttachment[] Attachments { get; set; }
    }

    public class TeamsAttachment
    {
        public string ContentType { get; set; }
        public object Content { get; set; }
    }
}