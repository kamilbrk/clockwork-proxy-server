using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ClockworkProxy.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using ClockworkProxy.Constants;

namespace ClockworkProxy
{
    public static class MessageIn
    {
        [FunctionName("messagein")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req, [Table("messages", Connection = "TableStorageAccount")]ICollector<ClockworkMessageStorage> messagesTable, TraceWriter log)
        {
            var data = ProcessRequest(await req.Content.ReadAsFormDataAsync());

            log.Info($"Recieved message from {data.From}");

            var type = data.Content.Substring(1, 1).Equals("m", StringComparison.InvariantCultureIgnoreCase) ? MessageTypes.Message : MessageTypes.Registration;

            var row = new ClockworkMessageStorage
            {
                PartitionKey = type,
                RowKey = Guid.NewGuid().ToString(),
                Id = HashFrom(data.From),
                From = data.From,
                To = data.To,
                Content = data.Content.Substring(2, data.Content.Length - 2),
                Sequence = data.Content.Substring(0, 1),
                Keyword = data.Keyword
            };

            messagesTable.Add(row);

            log.Info($"{type} {row.Sequence} from {data.From} inserted OK");

            return req.CreateResponse(HttpStatusCode.OK);
        }

        public static ClockworkMessageStorage ProcessRequest(NameValueCollection data)
        {
            return new ClockworkMessageStorage
            {
                To = data["To"],
                From = data["From"],
                Content = data["Content"],
                Keyword = data["Keyword"]
            };
        }

        public static string HashFrom(string from)
        {
            HashAlgorithm algorithm = SHA256.Create();
            var fromBytes = Encoding.UTF8.GetBytes(from);
            var fromHashBytes = algorithm.ComputeHash(fromBytes);
            return Convert.ToBase64String(fromHashBytes);
        }
    }
}
