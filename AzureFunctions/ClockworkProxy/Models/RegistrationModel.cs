using Newtonsoft.Json;

namespace ClockworkProxy.Models
{
    public class RegistrationModel
    {
        [JsonProperty(PropertyName = "mobile")]
        public string Mobile { get; set; }
        [JsonProperty(PropertyName = "public_key")]
        public string PublicKey { get; set; }
    }
}
