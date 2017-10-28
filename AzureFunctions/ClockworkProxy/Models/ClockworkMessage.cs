namespace ClockworkProxy.Models
{
    public class ClockworkMessage
    {
        public string Sequence { get; set; } 
        public string To { get; set; }
        public string From { get; set; }
        public string Content { get; set; }
        public string Id { get; set; }
        public string Keyword { get; set; }
    }
}
