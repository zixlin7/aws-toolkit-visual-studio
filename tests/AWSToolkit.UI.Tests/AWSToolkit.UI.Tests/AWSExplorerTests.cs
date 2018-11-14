using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UITesting.WinControls;
using Microsoft.VisualStudio.TestTools.UITesting.WpfControls;
using System.Threading;

namespace AWSToolkitUITests
{
    /// <summary>
    /// Summary description for AWSToolkitUITests
    /// </summary>
    [CodedUITest]
    public class AWSExplorerTests
    {
        public AWSExplorerTests()
        {
        }

        [TestMethod]
        public void CanCycleBetweenRegions()
        {
            #region Variable Declarations
            WinTitleBar uIAWSStudioTitleBar = this.UIMap.UIAWSStudioWindow.UIAWSStudioTitleBar;
            WpfTree uIResourceTreeTree = this.UIMap.UIAWSStudioWindow.UIAWSExplorerCustom.UIResourceTreeTree;
            WpfComboBox uIRegionsPickerComboBox = this.UIMap.UIAWSStudioWindow.UIAWSExplorerCustom.UIRegionsPickerComboBox;
            #endregion

            var app = LaunchTestShell();

            try
            {
                var regions = uIRegionsPickerComboBox.Items;
                for (var i = 0; i < regions.Count; i++)
                {
                    uIRegionsPickerComboBox.SelectedIndex = i;
                }

                // reset state for next run
                uIRegionsPickerComboBox.SelectedIndex = 0;
            }
            finally
            {
                ShutdownShell(app);
            }
        }

        [TestMethod]
        public void CanCycleBetweenAccounts()
        {
            #region Variable Declarations
            WinTitleBar uIAWSStudioTitleBar = this.UIMap.UIAWSStudioWindow.UIAWSStudioTitleBar;
            WpfTree uIResourceTreeTree = this.UIMap.UIAWSStudioWindow.UIAWSExplorerCustom.UIResourceTreeTree;
            WpfComboBox uiProfilesComboBox = this.UIMap.UIAWSStudioWindow.UIAWSExplorerCustom.UIRegisteredProfilesPiCustom.UI_ctlComboComboBox;
            #endregion

            var app = LaunchTestShell();

            try
            {
                var profiles = uiProfilesComboBox.Items;
                for (var i = 0; i < profiles.Count; i++)
                {
                    uiProfilesComboBox.SelectedIndex = i;
                }

                // reset state for next run
                uiProfilesComboBox.SelectedIndex = 0;
            }
            finally
            {
                ShutdownShell(app);
            }
        }

        private ApplicationUnderTest LaunchTestShell()
        {
            var app = ApplicationUnderTest.Launch(TestUtils.AWSStudioExe);

            // let the app stabilize in terms of downloading endpoints etc
            Thread.Sleep(5000);

            return app;
        }

        private static void ShutdownShell(ApplicationUnderTest app)
        {
            app.Close();
        }

        #region Additional test attributes

        // You can use the following additional attributes as you write your tests:

        ////Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{        
        //    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
        //}

        ////Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{        
        //    // To generate code for this test, select "Generate Code for Coded UI Test" from the shortcut menu and select one of the menu items.
        //}

        #endregion

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
        private TestContext testContextInstance;

        public UIMap UIMap
        {
            get
            {
                if (this.map == null)
                {
                    this.map = new UIMap();
                }

                return this.map;
            }
        }

        private UIMap map;
    }
}
