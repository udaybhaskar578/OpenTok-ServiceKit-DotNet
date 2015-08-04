using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Services.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Collections.ObjectModel;
/***************************************************
 * Helper functions to access the TableStorage
 * *************************************************/

namespace ServiceKit_DotNet.Utilities
{
    public class TableHelper {
        TableServiceContext context;
        string table;

        public TableHelper() { }

        public TableHelper(string connection, string tableName) {
            var account = CloudStorageAccount.Parse(connection);
            var retry = new LinearRetry(TimeSpan.FromSeconds(1), 3);
            var tableClient = account.CreateCloudTableClient();
            tableClient.RetryPolicy = retry;
            var cloudTable = tableClient.GetTableReference(tableName);
            cloudTable.CreateIfNotExists();
            context = new TableServiceContext(tableClient);
            table = tableName;
        }

        //Inserts
        public void Insert<T>(T entity) where T : TableServiceEntity {
            context.AddObject(table, entity);
            context.SaveChangesWithRetries();
        }

        public bool DeleteIfPresent<T>(T entity, bool attach) where T : TableServiceEntity
        {
            try
            {
                if (attach) context.AttachTo(table, entity, "*");
                context.DeleteObject(entity);
                context.SaveChangesWithRetries();
                return true;
            }
            catch (StorageException stex)
            {
                if (stex.Message != "ResourceNotFound")
                {
                    throw;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw;
            }
        }

        public void InsertOrReplace<T>(T entity) where T : TableServiceEntity
        {
            var e = GetEntity<T>(entity.PartitionKey, entity.RowKey);
            if (e == null)
            {
                context.AddObject(table, entity);
            }
            else
            {
                context.Detach(e);
                context.AttachTo(table, entity, "*");
                context.UpdateObject(entity);
            }
            context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
        }

        public T GetEntity<T>(string partitionKey, string row = null) where T : TableServiceEntity
        {
            try
            {
                T entity = null;
                IQueryable<T> query;

                query = (from e in context.CreateQuery<T>(table)
                         where e.PartitionKey == partitionKey
                         select e);

                if (!string.IsNullOrEmpty(row))
                {
                    query = query.Where(e => e.RowKey == row);
                }

                entity = query.AsTableServiceQuery<T>(context).FirstOrDefault();
                return entity;
            }
            catch (DataServiceRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("<code>ResourceNotFound</code>"))
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public bool InsertIfNotExists<T>(T entity) where T : TableServiceEntity {
            try {
                context.AddObject(table, entity);
                context.SaveChangesWithRetries();
                return true;
            } catch (StorageException stex) {
                if (stex.Message != "EntityAlreadyExists") {
                    throw;
                } else {
                    return false;
                }
            } catch {
                throw;
            }
        }
       
        public void InsertObject<T>(T entity) where T : TableServiceEntity {
            context.AddObject(table, entity);
        }
     
        //Updates
        public void Update<T>(T entity, bool attach) where T : TableServiceEntity {
            if (attach) context.AttachTo(table, entity, "*");
            context.UpdateObject(entity);
            context.SaveChangesWithRetries();
        }
       
        //Deletes
        public void Delete<T>(T entity, bool attach) where T : TableServiceEntity {
            if (attach) context.AttachTo(table, entity, "*");
            context.DeleteObject(entity);
            context.SaveChangesWithRetries();
        }

        //Save
        public void SaveChanges(SaveChangesOptions options) {
            context.SaveChangesWithRetries(options);
        }

        public ReadOnlyCollection<EntityDescriptor> SaveChangesWithRetry(SaveChangesOptions options, Delegate method, params object[] args) {
            int retries = 3;
            TimeSpan interval = TimeSpan.FromSeconds(1);
            int retry = 0;
            Exception lastErr = null;
            var success = false;
            TableServiceContext localContext = null;

            while (retry < retries && !success) {
                try {
                    localContext = (TableServiceContext)method.DynamicInvoke(args);
                    if (localContext == null) return null;
                    localContext.SaveChanges(options);
                    lastErr = null;
                    success = true;
                } catch (Exception err) {
                    System.Threading.Thread.Sleep(interval);
                    lastErr = err;
                    retry++;
                }
            }

            if (lastErr != null) {
                throw new Exception(lastErr.Message, lastErr.InnerException);
            }

            if (localContext != null) {
                return localContext.Entities;
            } else {
                return null;
            }
        }

     
        public TableServiceContext GetContext() {
            return context;
        }
    }
}

public class RowKeyType : TableServiceEntity {
    public RowKeyType() : base() { }

    public RowKeyType(string partitionKey, string rowKey)
        : base(partitionKey, rowKey) { }
}