using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace $safeprojectname$
{
    public static class TableOperations
    {
        static readonly string[] SAMPLE_TABLE_NAMES = { "Profiles" };
        /// <summary>
        /// Creates all samples defined in SampleTables map
        /// </summary>
        /// <param name="client"></param>
        public static void CreateSampleTables(AmazonDynamoDBClient client)
        {
            Console.WriteLine("Getting list of tables");
            List<string> currentTables = client.ListTables().TableNames;
            Console.WriteLine("Number of tables: " + currentTables.Count);

            bool tablesAdded = false;
            if (!currentTables.Contains("Profiles"))
            {
                Console.WriteLine("Table Profiles does not exist, creating");
                client.CreateTable(new CreateTableRequest
                {
                    TableName = "Profiles",
                    ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 3, WriteCapacityUnits = 1 },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Name",
                            KeyType = KeyType.HASH
                        }
                    },
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition { AttributeName = "Name", AttributeType = ScalarAttributeType.S }
                    }
                });
                tablesAdded = true;
            }

            if (tablesAdded)
            {
                bool allActive;
                do
                {
                    allActive = true;
                    Console.WriteLine("While tables are still being created, sleeping for 5 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    foreach (var tableName in SAMPLE_TABLE_NAMES)
                    {
                        TableStatus tableStatus = GetTableStatus(client, tableName);
                        if (!object.Equals(tableStatus, TableStatus.ACTIVE))
                            allActive = false;
                    }
                } while (!allActive);
            }

            Console.WriteLine("All sample tables created");
        }

        /// <summary>
        /// Retrieves a table status. Returns empty string if table does not exist.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static TableStatus GetTableStatus(AmazonDynamoDBClient client, string tableName)
        {
            try
            {
                var table = client.DescribeTable(new DescribeTableRequest { TableName = tableName }).Table;
                return (table == null) ? null : table.TableStatus;
            }
            catch (AmazonDynamoDBException db)
            {
                if (db.ErrorCode == "ResourceNotFoundException")
                    return string.Empty;
                throw;
            }
        }

        /// <summary>
        /// Deletes all sample tables
        /// </summary>
        /// <param name="client"></param>
        public static void DeleteSampleTables(AmazonDynamoDBClient client)
        {
            foreach (var table in SAMPLE_TABLE_NAMES)
            {
                Console.WriteLine("Deleting table " + table);
                client.DeleteTable(new DeleteTableRequest { TableName = table });
            }

            int remainingTables;
            do
            {
                Console.WriteLine("While sample tables still exist, sleeping for 5 seconds...");
                Thread.Sleep(TimeSpan.FromSeconds(5));

                Console.WriteLine("Getting list of tables");
                var currentTables = client.ListTables().TableNames;
                remainingTables = currentTables.Intersect(SAMPLE_TABLE_NAMES).Count();
            } while (remainingTables > 0);

            Console.WriteLine("Sample tables deleted");
        }
    }
}
