using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.SimpleDB.Model;

namespace Amazon.AWSToolkit.Tests.SimpleDB
{
    /// <summary>
    /// Summary description for DetermineDomainFromQuery
    /// </summary>
    [TestClass]
    public class DetermineDomainFromQuery
    {
        public DetermineDomainFromQuery()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void FindDomainFromQuery()
        {
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * FROM MyDomain"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from MyDomain"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from MyDomain where stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from MyDomain\twhere stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from\tMyDomain\twhere stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from\rMyDomain\rwhere stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from\nMyDomain\nwhere stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * from\r\nMyDomain\r\nwhere stuff"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * FROM `MyDomain`"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * FROM 'MyDomain'"));
            Assert.AreEqual("MyDomain", QueryBrowserModel.DetermineDomain("SELECT * FROM \"MyDomain\""));


            Assert.AreEqual("", QueryBrowserModel.DetermineDomain("Select *"));
            Assert.AreEqual("", QueryBrowserModel.DetermineDomain("Select * from"));
        }


    }
}
