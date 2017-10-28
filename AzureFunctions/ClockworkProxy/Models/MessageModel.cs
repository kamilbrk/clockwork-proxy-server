using Newtonsoft.Json;

namespace ClockworkProxy.Models
{
    public class MessageModel
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
