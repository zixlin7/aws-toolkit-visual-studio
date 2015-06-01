using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.Tests.Persistence
{
    /// <summary>
    /// Summary description for SettingsPersistenceTests
    /// </summary>
    [TestClass]
    public class SettingsPersistenceTests
    {
        public SettingsPersistenceTests()
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


        [TestMethod]
        public void SaveSettings()
        {
            string settingsName = "testSettings" + DateTime.Now.Ticks;
            SettingsCollection settings = PersistenceManager.Instance.GetSettings(settingsName);
            Assert.AreEqual(0, settings.Count);

            SettingsCollection.ObjectSettings values = settings.NewObjectSettings();
            values["key1"] = "value1";

            PersistenceManager.Instance.SaveSettings(settingsName, settings);

            settings = PersistenceManager.Instance.GetSettings(settingsName);
            Assert.AreEqual(1, settings.Count);
            Assert.AreEqual("value1", settings[values.UniqueKey]["key1"]);

            settings.Clear();
            PersistenceManager.Instance.SaveSettings(settingsName, settings);
        }
    }
}
