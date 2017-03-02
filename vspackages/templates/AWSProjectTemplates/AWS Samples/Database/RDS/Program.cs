/*******************************************************************************
* Copyright 2009-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.Runtime;
using System.Collections.Specialized;
using System.Configuration;

namespace $safeprojectname$
{
    class Program
    {
        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon RDS account for this to work.
        // See http://aws.amazon.com/rds/ for details on creating an Amazon RDS account.
        // This sample creates a Snapshot of an existing RDS DB and then restores it.
        // Change the dbInstanceIdentifier and dbSnapshotIdentifier fields to values 
        // that match your dbInstanceIdentifier and dbSnapshotIdentifier.

        static IAmazonRDS client;

        // Set the identifier for the existing DB from which the snapshot will be created.
        static string dbInstanceIdentifier = null;

        // Set the identifier for the snapshot to be created.
        static string dbSnapshotIdentifier = null;

        // Set the identifier for the new DB which will be created using the snapshot.
        static string newDbInstanceIdentifier = "RestoredDB";

        static void Main(string[] args)
        {
            if (CheckRequiredFields())
            {
                using (client = new AmazonRDSClient())
                {
                    try
                    {
                        // Create a snapshot
                        var snapshotRequest = new CreateDBSnapshotRequest
                        {
                            DBInstanceIdentifier = dbInstanceIdentifier,
                            DBSnapshotIdentifier = dbSnapshotIdentifier,

                        };
                        Console.WriteLine("Creating the DB Snapshot...");
                        var response = client.CreateDBSnapshot(snapshotRequest);

                        Console.WriteLine("Waiting for the DB Snapshot to be available...");
                        // Wait for the Snapshot to be available.
                        bool snapshotReady;
                        do
                        {
                            System.Threading.Thread.Sleep(1000 * 60);
                            snapshotReady = client.DescribeDBSnapshots(
                                new DescribeDBSnapshotsRequest { DBSnapshotIdentifier = dbSnapshotIdentifier })
                                .DBSnapshots[0].Status.Equals("available", StringComparison.InvariantCultureIgnoreCase);

                        } while (!snapshotReady);
                        Console.WriteLine("The DB Snapshot is available...");

                        // Restore a DB instance using the snapshot.
                        var restoreSnapshotRequest = new RestoreDBInstanceFromDBSnapshotRequest
                       {
                           DBSnapshotIdentifier = dbSnapshotIdentifier,
                           DBInstanceIdentifier = newDbInstanceIdentifier,
                       };
                        Console.WriteLine("Restoring new DB using the snapshot...");
                        client.RestoreDBInstanceFromDBSnapshot(restoreSnapshotRequest);

                        Console.WriteLine("Waiting for the DB to be restored...");
                        // Wait for the DB instance to be available.
                        bool dbInstanceReady;
                        do
                        {
                            System.Threading.Thread.Sleep(1000 * 60);
                            dbInstanceReady = client.DescribeDBInstances(
                                new DescribeDBInstancesRequest { DBInstanceIdentifier = newDbInstanceIdentifier })
                                .DBInstances[0].DBInstanceStatus.Equals("available", StringComparison.InvariantCultureIgnoreCase);

                        } while (!dbInstanceReady);
                        Console.WriteLine("The DB is restored.");


                    }
                    catch (AmazonRDSException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    catch (AmazonServiceException e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    Console.Write("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static bool CheckRequiredFields()
        {
            var appConfig = ConfigurationManager.AppSettings;

            if (string.IsNullOrEmpty(appConfig["AWSProfileName"]))
            {
                Console.WriteLine("AWSProfileName was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(dbInstanceIdentifier))
            {
                Console.WriteLine("The variable dbInstanceIdentifier is not set.");
                return false;
            }
            if (string.IsNullOrEmpty(dbSnapshotIdentifier))
            {
                Console.WriteLine("The variable dbSnapshotIdentifier is not set.");
                return false;
            }
            if (string.IsNullOrEmpty(newDbInstanceIdentifier))
            {
                Console.WriteLine("The variable newDbInstanceIdentifier is not set.");
                return false;
            }

            return true;
        }
    }
}
