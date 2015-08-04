using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using ServiceKit_DotNet.Utilities;

namespace ServiceKit_DotNet.data
{
    // Declaring a data type for help session
    public class HelpSessionDataType : TableServiceEntity
    {
        public HelpSessionDataType() : base() {}
        public HelpSessionDataType(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
        public string CustomerName { get; set; }
        public string ProblemText { get; set; }
        public string SessionId { get; set; }
    }

    public class HelpSessionData
    {
        private static string tablename = "helpsession";
        // Get help session from Table storage
        public static HelpSessionDataType GetHelpSession(string sessionId , string rowkey = "")
        {
            var helper = new TableHelper(Settings.StorageAccountConnection, tablename);
            var entity = helper.GetEntity<HelpSessionDataType>(sessionId, rowkey );
            return entity;
        }
        //Insert help session into the table storage
        public static void InsertOrUpdate(HelpSessionDataType entity)
        {
            //entity.PartitionKey = Settings.MainHost;
            entity.RowKey = "";

            var helper = new TableHelper(Settings.StorageAccountConnection, tablename);
            helper.InsertOrReplace<HelpSessionDataType>(entity);
        }

        // Delete a help session form the table storage
        public static void Delete(string sessionId)
        {
            var entity = new HelpSessionDataType();
            entity.PartitionKey = sessionId;
            entity.RowKey = "";
            var helper = new TableHelper(Settings.StorageAccountConnection, tablename);
            helper.DeleteIfPresent<HelpSessionDataType>(entity, true);
        }
    }
}

