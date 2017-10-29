using Microsoft.WindowsAzure.Storage.Table;

namespace ClockworkProxy.Models
{
    public class ClockworkMessageStorage : TableEntity
    {
        public string Sequence { get; set; } 
        public string To { get; set; }
        public string From { get; set; }
        public string Content { get; set; }
        public string Id { get; set; }
        public string Keyword { get; set; }
        public string MessageLength { get; set; }
    }
}
