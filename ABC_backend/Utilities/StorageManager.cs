//_______________________Class containing static housekeeping properties and functions, including reading and writing data___________

using ABC_backend.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABC_backend
{
    public static class StorageManager
    {
        static readonly String DEP_TABLE_NAME = "Departments";
        static readonly String ITEM_TABLE_NAME = "Items";
        static readonly CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureStorageConnectionString-1"));
        public static String DepartmentInUse { get; set; }

        //Accepts a containername as String and returns the relevant CloudBlobContainer
        public static CloudBlobContainer GetCloudBlobContainer(String containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("AzureStorageConnectionString-1"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            return container;
        }

        //Returns the cloud table where items are stored
        public static CloudTable GetCloudTable()
        {

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(ITEM_TABLE_NAME);
            return table;
        }

        //Accepts a Department object and adds it to the departments table
        public static void AddDepartment(Department department)
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(DEP_TABLE_NAME);
            table.CreateIfNotExists();
            TableOperation insertOperation = TableOperation.Insert(department);
            TableResult result = table.Execute(insertOperation);
        }

        //Returns the list of all departments in the departments table
        public static List<Department> GetDepartments()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(DEP_TABLE_NAME);
            TableQuery<Department> query = new TableQuery<Department>();
            List<Department> departmentsLst = new List<Department>();
            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<Department> resultSegment = table.ExecuteQuerySegmented(query, token);
                token = resultSegment.ContinuationToken;

                foreach (Department dept in resultSegment.Results)
                {
                    departmentsLst.Add(dept);
                }
            } while (token != null);
            return departmentsLst;
        }

        //Returns a list of all Items in the items table
        public static List<Item> GetItems()
        {
            CloudTable table = GetCloudTable();
            TableQuery<Item> query = new TableQuery<Item>();
            List<Item> itemsLst = new List<Item>();
            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<Item> resultSegment = table.ExecuteQuerySegmented(query, token);
                token = resultSegment.ContinuationToken;

                foreach (Item item in resultSegment.Results)
                {
                    itemsLst.Add(item);
                }
            } while (token != null);
            return itemsLst;
        }

        //Accepts a Rowkey as string and returns the specified item from the items table
        public static Item GetItem(String RowKey)
        {
            CloudTable table = GetCloudTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Item>(DepartmentInUse, RowKey);
            TableResult item = table.Execute(retrieveOperation);

            return item.Result as Item;
        }

        //Accepts an item and updates the record in items table
        public static void UpdateItem(Item item)
        {
            item.ETag = "*";
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(ITEM_TABLE_NAME);
            TableOperation updateOperation = TableOperation.Replace(item);
            table.Execute(updateOperation);
        }

        //Accepts an item and deletes the record from items table
        public static void DeleteItem(Item item)
        {
            item.ETag = "*";
            CloudTable table = GetCloudTable();
            TableOperation deleteOperation = TableOperation.Delete(item);
            table.Execute(deleteOperation);
        }

        //Accepts an item and adds the record to items table
        public static void AddItem(Item item)
        {
            item.ETag = "*";
            CloudTable table = GetCloudTable();
            TableOperation insertOperation = TableOperation.Insert(item);
            table.Execute(insertOperation);
        }
    }
}