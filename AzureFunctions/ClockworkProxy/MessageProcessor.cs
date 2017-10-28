using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ClockworkProxy.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockworkProxy
{
    public static class MessageProcessor
    {
        [FunctionName("MessageProcessor")]
        public static async void Run([TimerTrigger("*/15 * * * * *")]TimerInfo myTimer, [Table("messages", Connection = "TableStorageAccount")]IQueryable<ClockworkMessageStorage> messagesTable, [Table("messages", Connection = "TableStorageAccount")]CloudTable messagesCloudTable, TraceWriter log)
        {
            log.Info($"MessageProcessor running");

            var messageSets = messagesTable.ToArray().GroupBy(m => m.PartitionKey); //nasty!
            
            foreach (var messageSet in messageSets)
            {
                var orderedMessageSet = messageSet.OrderBy(m => m.Sequence);

                var batchOperation = new TableBatchOperation();
                var content = new StringBuilder();

                foreach (var message in orderedMessageSet)
                {
                    content.Append(message.Content);
                    batchOperation.Delete(message);
                }

                //TODO send content somewhere...

                await messagesCloudTable.ExecuteBatchAsync(batchOperation);
            }
        }
    }
}
