using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ClockworkProxy.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClockworkProxy.Constants;
using Newtonsoft.Json;

namespace ClockworkProxy
{
    public static class MessageProcessor
    {
        [FunctionName("MessageProcessor")]
        public static async void Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, [Table("messages", Connection = "TableStorageAccount")]IQueryable<ClockworkMessageStorage> messagesTable, [Table("messages", Connection = "TableStorageAccount")]CloudTable messagesCloudTable, TraceWriter log)
        {
            log.Info($"MessageProcessor running");

            var opperations = new List<TableBatchOperation>();

            var messages = messagesTable
                .Where(m => m.PartitionKey.Equals(MessageTypes.Message, StringComparison.InvariantCultureIgnoreCase))
                //.ToArray()
                .OrderBy(m => m.Sequence)
                .GroupBy(m => m.Id)
                .ToArray();

            log.Info($"Found {messages.Length} message sets to process");

            var registrations = messagesTable
                .Where(m => m.PartitionKey.Equals(MessageTypes.Registration, StringComparison.InvariantCultureIgnoreCase))
                //.ToArray()
                .OrderBy(m => m.Sequence)
                .GroupBy(m => m.From)
                .ToArray();

            log.Info($"Found {messages.Length} registrations to process");

            opperations.AddRange(await ProccessMessages(registrations, MessageTypes.Registration, log));
            opperations.AddRange(await ProccessMessages(messages, MessageTypes.Message, log));

            foreach (var opperation in opperations)
            {
                await messagesCloudTable.ExecuteBatchAsync(opperation);
            }
        }

        public static async Task<List<TableBatchOperation>> ProccessMessages(IEnumerable<IGrouping<string, ClockworkMessageStorage>> messageGroups, string messageType, TraceWriter log)
        {

            var opperations = new List<TableBatchOperation>();

            foreach (var messageGroup in messageGroups)
            {
                log.Info($"Processing {messageType} group {messageGroup.Key}");

                var batchOperation = new TableBatchOperation();
                var content = new StringBuilder();

                foreach (var message in messageGroup)
                {
                    log.Info($">>Inserting {message.Sequence}");
                    content.Append(message.Content);
                    batchOperation.Delete(message);
                }

                opperations.Add(batchOperation);

                switch (messageType)
                {
                    case MessageTypes.Message:
                        log.Info($"Sending message body '{content}'");

                        var messageJson = JsonConvert.SerializeObject(new MessageModel
                        {
                            Message = content.ToString()
                        });

                        using (var client = new HttpClient())
                        {
                           await client.PostAsync("http://163.172.129.168:3000/message", new StringContent(messageJson, Encoding.UTF8, "application/json"));
                        }
                        break;
                    case MessageTypes.Registration:
                        log.Info($"Sending registration body '{content}' for {messageGroup.Key}");

                        var registrationJson = JsonConvert.SerializeObject(new RegistrationModel
                        {
                            PublicKey = content.ToString(),
                            Mobile = messageGroup.Key
                        });

                        using (var client = new HttpClient())
                        {
                            await client.PostAsync("http://163.172.129.168:3000/register", new StringContent(registrationJson, Encoding.UTF8, "application/json"));
                        }
                        break;
                    default:
                        log.Warning($"Unknown message type '{messageType}'");
                        break;
                }
            }

            return opperations;
        }
    }
}
