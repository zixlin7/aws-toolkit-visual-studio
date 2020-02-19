using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;

using Amazon.RDS;
using Amazon.RDS.Model;
using System.ComponentModel;
using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class LaunchDBInstanceEnginePageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LaunchDBInstanceEnginePageController));

        private static readonly IList<string> DatabaseEnginePreferredOrder
            = new ReadOnlyCollection<string>(new List<string>
            {
                "sqlserver-ex", "sqlserver-web", "sqlserver-se", "sqlserver-ee",
                "oracle-se", "oracle-se1", "oracle-ee"
            });

        LaunchDBInstanceEnginePage _pageUI;
        Dictionary<string, List<DBEngineVersionWrapper>> _engines;

        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Engine Selection";

        public string ShortPageTitle => null;

        public string PageDescription => "Choose a database engine for your new instance.";

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

                _pageUI.AvailableEngineTypes = GetAvailableEngineTypes();
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
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward,
                QueryFinishButtonEnablement());
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
            Dictionary<string, List<DBEngineVersionWrapper>> engines =
                new Dictionary<string, List<DBEngineVersionWrapper>>();

            try
            {
                IAmazonRDS rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
                string nextMarker = null;
                do
                {
                    var request = new DescribeDBEngineVersionsRequest
                    {
                        Marker = nextMarker,
                        MaxRecords = 100
                    };
                    var response = rdsClient.DescribeDBEngineVersions(request);
                    nextMarker = response.Marker;

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
                } while (!string.IsNullOrEmpty(nextMarker));
            }
            catch (AmazonRDSException e)
            {
                LOGGER.ErrorFormat("DescribeEngineVersions exception: {0}", e.Message);
            }

            return engines;
        }

        private IList<DBEngineType> GetAvailableEngineTypes()
        {
            var candidateEngineTypes = _engines
                // Filter out any DB Engine Type that we don't have metadata for
                .Where(engineKeyValue => engineKeyValue.Value.Any())
                .Where(engineKeyValue => RDSServiceMeta.Instance.MetaForEngine(engineKeyValue.Key, false) != null)
                // Transform to the model of interest.
                .Select(engineKeyValue => new DBEngineType
                    {Title = engineKeyValue.Key, Description = engineKeyValue.Value.First().Description})
                .ToList();

            return candidateEngineTypes
                .OrderBy(engine => GetPreferredDatabaseEngineOrder(engine.Title))
                .ThenBy(engine => engine.Description)
                .ToList();
        }

        private static int GetPreferredDatabaseEngineOrder(string engineId)
        {
            var index = DatabaseEnginePreferredOrder.IndexOf(engineId);

            if (index == -1)
            {
                return DatabaseEnginePreferredOrder.Count;
            }

            return index;
        }
    }
}
