using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManager.Persistence;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class PersistenceTests
    {
        [TestInitialize]
        public void testSetup()
        {
            PersistenceManager.ResetDatabaseForTesting();
        }

        [TestMethod]
        public void BasicSaveAndLoad()
        {
            PersistenceManager manager = new PersistenceManager();
            var eo = new EntityObject(EntityType.Event);
            eo.Parameters["Key"] = "Value";
            eo.Status = "Success";
            manager.Persist(eo);

            var loaded = manager.Load(EntityType.Event, eo.Id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(eo.Id, loaded.Id);
            Assert.AreEqual(eo.Status, loaded.Status);
            Assert.AreEqual(eo.Timestamp, loaded.Timestamp);
            Assert.AreEqual(1, loaded.Parameters.Count);
            Assert.AreEqual("Value", loaded.Parameters["Key"]);

            DateTime originalTimestamp = eo.Timestamp;
            eo.Parameters["NewValue"] = "this is true";
            manager.Persist(eo);
            Assert.IsTrue(eo.Timestamp > originalTimestamp);

            loaded = manager.Load(EntityType.Event, eo.Id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(eo.Id, loaded.Id);
            Assert.AreEqual(eo.Status, loaded.Status);
            Assert.AreEqual(eo.Timestamp, loaded.Timestamp);
            Assert.AreEqual(2, loaded.Parameters.Count);
            Assert.AreEqual("Value", loaded.Parameters["Key"]);
            Assert.AreEqual("this is true", loaded.Parameters["NewValue"]);
        }

        [TestMethod]
        public void LoadLatestWithEmptyTable()
        {
            PersistenceManager.ResetDatabaseForTesting();
            var found = new PersistenceManager().LoadLatest(EntityType.Event);
            Assert.IsNull(found);
        }

        [TestMethod]
        public void LoadLatestOfManyItems()
        {
            PersistenceManager manager = new PersistenceManager();
            for (int i = 0; i < 10; i++)
            {
                var eo = new EntityObject(EntityType.Event);
                manager.Persist(eo);
                Thread.Sleep(100);
            }

            var last = new EntityObject(EntityType.Event);
            last.Status = "This is the Last";
            last.Parameters["data"] = "silly";
            manager.Persist(last);

            var found = manager.LoadLatest(EntityType.Event);
            Assert.AreEqual(last.Status, found.Status);
            Assert.AreEqual("silly", found.Parameters["data"]);
        }

        [TestMethod]
        public void SelectByStatusNotFound()
        {
            var found = new PersistenceManager().SelectByStatus(EntityType.Event, "NotExist");
            Assert.AreEqual(0, found.Count);
        }

        [TestMethod]
        public void SelectByStatusWithMatchingRecords()
        {
            PersistenceManager manager = new PersistenceManager();
            for (int i = 0; i < 10; i++)
            {
                var eo = new EntityObject(EntityType.Event);
                eo.Status = (i % 2).ToString();
                eo.Parameters["data"] = "id";
                manager.Persist(eo);
            }

            IList<EntityObject> list = manager.SelectByStatus(EntityType.Event, "0");
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual("id", list[0].Parameters["data"]);
        }

        [TestMethod]
        public void SelectByTimestamp()
        {
            EntityObject middleObject = null;
            PersistenceManager manager = new PersistenceManager();
            for (int i = 0; i < 10; i++)
            {
                var eo = new EntityObject(EntityType.Event);
                eo.Status = (i % 2).ToString();
                eo.Parameters["data"] = "id";
                manager.Persist(eo);

                Thread.Sleep(100);

                if (i == 4)
                    middleObject = eo;
            }

            IList<EntityObject> list = manager.SelectByGreaterThanTimestamp(EntityType.Event, middleObject.Timestamp);
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual("id", list[0].Parameters["data"]);

            foreach (var eo in list)
            {
                Assert.IsTrue(eo.Timestamp > middleObject.Timestamp);
            }
        }
    }
}
