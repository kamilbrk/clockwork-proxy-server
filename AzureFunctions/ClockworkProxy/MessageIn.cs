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
using System.Text;
using ClockworkProxy.Constants;

namespace ClockworkProxy
{
    //  01rzMESSAGE
    //  0 = correlation(0-z) 0
    //   1 = sequence(0-z)
    //    r = type(m/r)
    //     z = last message(1-z)


    public static class MessageIn
    {
        [FunctionName("messagein")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req, [Table("messages", Connection = "TableStorageAccount")]ICollector<ClockworkMessageStorage> messagesTable, TraceWriter log)
        {
            var data = ProcessRequest(await req.Content.ReadAsFormDataAsync());

            log.Info($"Recieved message from {data.From}");


            var correlation = data.Content.Substring(0, 1);
            var sequence = data.Content.Substring(1, 1);
            var type = data.Content.Substring(2, 1).Equals("m", StringComparison.InvariantCultureIgnoreCase) ? MessageTypes.Message : MessageTypes.Registration;
            var length = data.Content.Substring(3, 1);
            

            var identity = HashFrom($"{correlation}{data.From}")
                .Replace('/', '$')
                .Replace('?', '£')
                .Replace('#', '!')
                .Replace('\\', '&');

            var partitionKey = $"{type}-{identity}";

            var row = new ClockworkMessageStorage
            {
                PartitionKey = partitionKey,
                RowKey = sequence,
                From = data.From,
                To = data.To,
                Content = data.Content.Substring(4, data.Content.Length - 4),
                MessageLength = length
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
