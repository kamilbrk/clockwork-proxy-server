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

namespace ClockworkProxy
{
    public static class MessageIn
    {
        [FunctionName("messagein")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req, [Table("messages", Connection = "TableStorageAccount")]ICollector<ClockworkMessageStorage> messagesTable, TraceWriter log)
        {
            var data = ProcessRequest(await req.Content.ReadAsFormDataAsync());
            var identity = HashFrom(data.From);

            if (data.Content.Length < 2)
            {
                log.Warning($"Recieved message '{identity}' body is too short");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Content is too short!");
            }

            log.Info($"Recieved message '{identity}'");

            var sequence = data.Content.Substring(0, 1);
            var content = data.Content.Substring(1, data.Content.Length - 1);

            var row = new ClockworkMessageStorage
            {
                PartitionKey = identity,
                RowKey = Guid.NewGuid().ToString(),
                To = data.To,
                Content = content,
                Sequence = sequence,
                Keyword = data.Keyword
            };

            messagesTable.Add(row);

            log.Info($"Message'{identity}' inserted 'OK'");

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
