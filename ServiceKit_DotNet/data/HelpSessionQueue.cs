using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Queue;
using ServiceKit_DotNet.utilities;

namespace ServiceKit_DotNet.data
{
    public class HelpSessionQueue
    {
        private const string QueueName = "helpqueue";
        
        // Insert message into CloudQueue
        public static void Insert(string message)
        {
            var helper = new QueueHelper(Settings.StorageAccountConnection, QueueName);
            helper.AddMessage(message);
        }

        // Get the first available message in the CloudQueue
        public static CloudQueueMessage GetMessage()
        {
            var helper = new QueueHelper(Settings.StorageAccountConnection, QueueName);
            var message = helper.GetMessage();
            return message;
        }

        // Get all the messages in the queue
        public static List<CloudQueueMessage> GetMessages()
        {
            var helper = new QueueHelper(Settings.StorageAccountConnection, QueueName);
            var messages = helper.GetMessages();
            return messages.ToList();
        }

        // Delete a message form CloudQueue
        public static void DeleteMessage(CloudQueueMessage message)
        {
            var helper = new QueueHelper(Settings.StorageAccountConnection, QueueName);
            helper.DeleteMessage(message);
        }
    }
}