using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;

using Amazon.RDS;
using Amazon.RDS.Model;
using System.ComponentModel;
using Amazon.AWSToolkit.RDS.Nodes;

using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class LaunchDBInstanceEnginePageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LaunchDBInstanceEnginePageController));
        LaunchDBInstanceEnginePage _pageUI;
        Dictionary<string, List<DBEngineVersionWrapper>> _engines;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard  { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Engine Selection"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Choose a database engine for your new instance."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new LaunchDBInstanceEnginePage();
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
                _engines = QueryAvailableEngineVersions();

                // transform to a shorter collection of types for the page list, using the
                // first available engine version of each type for data but order so we get
                // sql server first, then oracle and then the remainder
                var priorityOrderKeyList = new[] 
                {
                    "sqlserver-ex", "sqlserver-web", "sqlserver-se", "sqlserver-ee",
                    "oracle-se", "oracle-se1", "oracle-ee"
                };
                var priorityOrderKeys = new HashSet<string>(priorityOrderKeyList);

                var engineTypes = (from key in priorityOrderKeyList
                                   where _engines.ContainsKey(key)
                                   let wrappers = _engines[key]
                                   where wrappers.Count > 0
                                   select new DBEngineType { Title = key, Description = wrappers[0].Description })
                                   .ToList();
                engineTypes.AddRange(from engine in _engines.Keys
                                     where (!priorityOrderKeys.Contains(engine))
                                     let wrappers = _engines[engine]
                                     where wrappers.Count > 0
                                     select new DBEngineType { Title = engine, Description = wrappers[0].Description });

                _pageUI.AvailableEngineTypes = engineTypes;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            List<DBEngineVersionWrapper> engineVersions = _engines[_pageUI.SelectedEngineType];
            HostingWizard[RDSWizardProperties.SeedData.propkey_DBEngineVersions] = engineVersions;
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return _pageUI != null && !string.IsNullOrEmpty(_pageUI.SelectedEngineType);
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, QueryFinishButtonEnablement());
        }

        public bool AllowShortCircuit()
        {
            return QueryFinishButtonEnablement();
        }

        #endregion

        void _pageUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        /// <summary>
        /// Queries all available db engines and versions and returns collection
        /// grouped by engine name. The keys of this collection can be used to
        /// drive the this page (initial engine type selection) and the associated
        /// list can then be used to drive subsequent pages.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, List<DBEngineVersionWrapper>> QueryAvailableEngineVersions()
        {
            Dictionary<string, List<DBEngineVersionWrapper>> engines = new Dictionary<string, List<DBEngineVersionWrapper>>();

            try
            {
                IAmazonRDS rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
                DescribeDBEngineVersionsResponse response = rdsClient.DescribeDBEngineVersions();

                foreach (var engine in response.DBEngineVersions)
                {
                    List<DBEngineVersionWrapper> wrappers;
                    if (engines.ContainsKey(engine.Engine))
                        wrappers = engines[engine.Engine];
                    else
                    {
                        wrappers = new List<DBEngineVersionWrapper>();
                        engines.Add(engine.Engine, wrappers);
                    }
                    wrappers.Add(new DBEngineVersionWrapper(engine));
                }
            }
            catch (AmazonRDSException e)
            {
                LOGGER.ErrorFormat("DescribeEngineVersions exception: {0}", e.Message);
            }

            return engines;
        }
    }
}
