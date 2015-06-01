using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.Tests.EC2
{
    /// <summary>
    /// Summary description for ColumnDefinitionTests
    /// </summary>
    [TestClass]
    public class ColumnDefinitionTests
    {
        public ColumnDefinitionTests()
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
        public void ListImageDefinitions()
        {
            var cols = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(ImageWrapper)); ;
            Assert.IsTrue(cols.Length > 0);

            var amiId = cols.FirstOrDefault(x => x.FieldName == "Name");
            Assert.IsNotNull(amiId);
            Assert.IsFalse(amiId.IsIconDynamic);
            Assert.IsTrue(amiId.Icon.StartsWith("Amazon.AWSToolkit.EC2.Resources.EmbeddedImages"));

            var state = cols.FirstOrDefault(x => x.FieldName == "State");
            Assert.IsNotNull(state);
            Assert.IsTrue(state.IsIconDynamic);
            Assert.IsTrue(state.Icon.StartsWith("StateIcon"));
        }
    }
}
