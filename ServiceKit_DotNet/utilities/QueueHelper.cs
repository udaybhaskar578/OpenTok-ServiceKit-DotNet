using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
/***************************************************
 * Helper functions to access the CloudQueue
 * *************************************************/

namespace ServiceKit_DotNet.utilities
{
    public class QueueHelper
    {
        
        CloudQueueClient queueClient;
        CloudQueue queue;

        public QueueHelper(string connection, string queueName) {
            
            var account = CloudStorageAccount.Parse(connection);
            var retry = new LinearRetry(TimeSpan.FromSeconds(1), 3);
            queueClient = account.CreateCloudQueueClient();
            queueClient.RetryPolicy = retry;
            queue = queueClient.GetQueueReference(queueName);
            queue.CreateIfNotExists();
        }

        public void AddMessage(string message) {
            var queueMessage = new CloudQueueMessage(message);
            queue.AddMessage(queueMessage);
        }

        public CloudQueueMessage GetMessage() {
            CloudQueueMessage message = null;
            message = queue.GetMessage();
            return message;
        }

        public IEnumerable<CloudQueueMessage> GetMessages(int? count = null, TimeSpan? visibilityTimeout = null) {
            IEnumerable<CloudQueueMessage> messages = null;
            if (count == null) count = 32;
            if (visibilityTimeout == null) visibilityTimeout = TimeSpan.FromMinutes(5);
            messages = queue.GetMessages(count.Value, visibilityTimeout.Value);
            return messages;
        }

        public void DeleteMessage(CloudQueueMessage message)
        {
            queue.DeleteMessage(message);      
        }
    }

}
