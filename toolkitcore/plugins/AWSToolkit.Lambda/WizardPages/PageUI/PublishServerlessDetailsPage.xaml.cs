using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.Lambda;

using log4net;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PublishServerlessDetailsPage.xaml
    /// </summary>
    public partial class PublishServerlessDetailsPage : INotifyPropertyChanged
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PublishServerlessDetailsPage));
        public static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;

        private const int AccountRegionChangedDebounceMs = 250;

        public IAWSWizardPageController PageController { get; set; }
        public AccountAndRegionPickerViewModel Connection { get; }

        public ToolkitContext ToolkitContext { get; set; }

        public PublishServerlessViewModel ViewModel { get; }

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        public PublishServerlessDetailsPage() : this(ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public PublishServerlessDetailsPage(ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
            InitializeComponent();

            ViewModel = new PublishServerlessViewModel(toolkitContext);

            ViewModel.Connection.SetServiceFilter(new List<string>() { LambdaServiceName });

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            DataContext = this;
        }


        public PublishServerlessDetailsPage(IAWSWizardPageController pageController, ToolkitContext toolkitContext) :
            this(toolkitContext)
        {
            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var userAccount = hostWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
            var region = hostWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

            ViewModel.Connection.Account = userAccount;
            ViewModel.Connection.Region = region;

            UpdateExistingResources();

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.S3Bucket))
            {
                ViewModel.S3Bucket = hostWizard[UploadFunctionWizardProperties.S3Bucket] as string;
            }

            if (hostWizard.IsPropertySet(UploadFunctionWizardProperties.StackName))
            {
                ViewModel.Stack = hostWizard[UploadFunctionWizardProperties.StackName] as string;
            }

            ViewModel.SaveSettings = true;
        }


        private void ConnectionChanged(object sender, EventArgs e)
        {
            if (!ViewModel.Connection.ConnectionIsValid)
            {
                return;
            }

            PageController.HostingWizard.SetSelectedAccount(ViewModel.Connection.Account,
                UploadFunctionWizardProperties.UserAccount);
            PageController.HostingWizard.SetSelectedRegion(ViewModel.Connection.Region,
                UploadFunctionWizardProperties.Region);

            // Prevent multiple loads caused by property changed events in rapid succession
            _accountRegionChangeDebounceDispatcher.Debounce(AccountRegionChangedDebounceMs, _ =>
            {
                UpdateExistingResources();
            });
        }

        private void UpdateExistingResources()
        {
            try
            {
                if (!ViewModel.Connection.ConnectionIsValid)
                {
                    return;
                }

                ViewModel.UpdateExistingResourcesAsync().LogExceptionAndForget();
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing existing resources", e);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            return;
        }
    }
}
