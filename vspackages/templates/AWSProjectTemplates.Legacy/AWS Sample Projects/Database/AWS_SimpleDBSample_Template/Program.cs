/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;

using Amazon;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Attribute = Amazon.SimpleDB.Model.Attribute;

namespace $safeprojectname$
{
    class Program
    {
        public static void Main(string[] args)
        {
            var sdb = new AmazonSimpleDBClient();

            try
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("Getting Started with Amazon SimpleDB");
                Console.WriteLine("===========================================\n");

                // Creating a domain
                Console.WriteLine("Creating domain called MyStore.\n");
                const string domainName = "MyStore";
                var createDomain = new CreateDomainRequest { DomainName = domainName };
                sdb.CreateDomain(createDomain);

                // Listing domains
                var sdbListDomainsResponse = sdb.ListDomains(new ListDomainsRequest());
                Console.WriteLine("List of domains:\n");
                foreach (var domain in sdbListDomainsResponse.DomainNames)
                {
                    Console.WriteLine("  " + domain);
                }
                Console.WriteLine();

                // Putting data into a domain
                Console.WriteLine("Putting data into MyStore domain.\n");
                const string itemNameOne = "Item_01";
                var putAttributesActionOne = new PutAttributesRequest { DomainName = domainName, ItemName = itemNameOne };
                var attributesOne = putAttributesActionOne.Attributes;
                attributesOne.Add(new ReplaceableAttribute { Name = "Category", Value = "Clothes" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Subcategory", Value = "Sweater" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Name", Value = "Cathair Sweater" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Color", Value = "Siamese" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Size", Value = "Small" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Size", Value = "Medium" });
                attributesOne.Add(new ReplaceableAttribute { Name = "Size", Value = "Large" });
                sdb.PutAttributes(putAttributesActionOne);

                const string itemNameTwo = "Item_02";
                var putAttributesActionTwo = new PutAttributesRequest { DomainName = domainName, ItemName = itemNameTwo };
                var attributesTwo = putAttributesActionTwo.Attributes;
                attributesTwo.Add(new ReplaceableAttribute { Name = "Category", Value = "Clothes" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Subcategory", Value = "Pants" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Name", Value = "Designer Jeans" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Color", Value = "Paisley Acid Wash" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Size", Value = "30x32" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Size", Value = "32x32" });
                attributesTwo.Add(new ReplaceableAttribute { Name = "Size", Value = "32x34" });
                sdb.PutAttributes(putAttributesActionTwo);

                const string itemNameThree = "Item_03";
                var putAttributesActionThree = new PutAttributesRequest { DomainName = domainName, ItemName = itemNameThree };
                var attributesThree = putAttributesActionThree.Attributes;
                attributesThree.Add(new ReplaceableAttribute { Name = "Category", Value = "Clothes" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Subcategory", Value = "Pants" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Name", Value = "Sweatpants" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Color", Value = "Blue" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Color", Value = "Yellow" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Color", Value = "Pink" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Size", Value = "Large" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Year", Value = "2006" });
                attributesThree.Add(new ReplaceableAttribute { Name = "Year", Value = "2007" });
                sdb.PutAttributes(putAttributesActionThree);

                const string itemNameFour = "Item_04";
                var putAttributesActionFour = new PutAttributesRequest { DomainName = domainName, ItemName = itemNameFour };
                var attributesFour = putAttributesActionFour.Attributes;
                attributesFour.Add(new ReplaceableAttribute { Name = "Category", Value = "Car Parts" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Subcategory", Value = "Engine" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Name", Value = "Turbos" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Make", Value = "Audi" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Model", Value = "S4" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Year", Value = "2000" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Year", Value = "2001" });
                attributesFour.Add(new ReplaceableAttribute { Name = "Year", Value = "2002" });
                sdb.PutAttributes(putAttributesActionFour);

                const string itemNameFive = "Item_05";
                var putAttributesActionFive = new PutAttributesRequest { DomainName = domainName, ItemName = itemNameFive };
                var attributesFive = putAttributesActionFive.Attributes;
                attributesFive.Add(new ReplaceableAttribute { Name = "Category", Value = "Car Parts" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Subcategory", Value = "Emissions" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Name", Value = "O2 Sensor" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Make", Value = "Audi" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Model", Value = "S4" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Year", Value = "2000" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Year", Value = "2001" });
                attributesFive.Add(new ReplaceableAttribute { Name = "Year", Value = "2002" });
                sdb.PutAttributes(putAttributesActionFive);

                // Getting data from a domain
                Console.WriteLine("Print attributes with the attribute Category that contain the value Clothes.\n");
                const string selectExpression = "Select * From MyStore Where Category = 'Clothes'";
                var selectRequestAction = new SelectRequest { SelectExpression = selectExpression };
                var selectResponse = sdb.Select(selectRequestAction);

                foreach (var item in selectResponse.Items)
                {
                    Console.WriteLine("  Item");
                    if (!string.IsNullOrEmpty(item.Name))
                    {
                        Console.WriteLine("    Name: {0}", item.Name);
                    }
                    foreach (var attribute in item.Attributes)
                    {
                        Console.WriteLine("      Attribute");
                        if (!string.IsNullOrEmpty(attribute.Name))
                        {
                            Console.WriteLine("        Name: {0}", attribute.Name);
                        }
                        if (!string.IsNullOrEmpty(attribute.Value))
                        {
                            Console.WriteLine("        Value: {0}", attribute.Value);
                        }
                    }
                }
                Console.WriteLine();

                // Deleting values from an attribute
                Console.WriteLine("Deleting Blue attributes in Item_O3.\n");
                var deleteValueAttribute = new Amazon.SimpleDB.Model.Attribute { Name = "Color", Value = "Blue" };
                var deleteValueAction = new DeleteAttributesRequest
                    {
                        DomainName = "MyStore",
                        ItemName = "Item_03",
                        Attributes = new List<Attribute> { deleteValueAttribute }
                    };
                sdb.DeleteAttributes(deleteValueAction);

                //Deleting an attribute
                Console.WriteLine("Deleting attribute Year in Item_O3.\n");
                var deleteAttribute = new Amazon.SimpleDB.Model.Attribute { Name = "Year" };
                var deleteAttributeAction = new DeleteAttributesRequest
                    {
                        DomainName = "MyStore",
                        ItemName = "Item_03",
                        Attributes = new List<Attribute> { deleteAttribute }
                    };
                sdb.DeleteAttributes(deleteAttributeAction);

                //Replacing an attribute
                Console.WriteLine("Replace Size of Item_03 with Medium.\n");
                var replaceableAttribute = new ReplaceableAttribute { Name = "Size", Value = "Medium", Replace = true };
                var replaceAction = new PutAttributesRequest
                    {
                        DomainName = "MyStore",
                        ItemName = "Item_03",
                        Attributes = new List<ReplaceableAttribute> { replaceableAttribute }
                    };
                sdb.PutAttributes(replaceAction);

                //Deleting an item
                Console.WriteLine("Deleting Item_03 item.\n");
                var deleteItemAction = new DeleteAttributesRequest { DomainName = "MyStore", ItemName = "Item_03" };
                sdb.DeleteAttributes(deleteAttributeAction);

                //Deleting a domain
                Console.WriteLine("Deleting MyStore domain.\n");
                var deleteDomainAction = new DeleteDomainRequest { DomainName = "MyStore" };
                sdb.DeleteDomain(deleteDomainAction);

            }
            catch (AmazonSimpleDBException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
            }

            Console.WriteLine("Press Enter to continue...");
            Console.Read();
        }
    }
}