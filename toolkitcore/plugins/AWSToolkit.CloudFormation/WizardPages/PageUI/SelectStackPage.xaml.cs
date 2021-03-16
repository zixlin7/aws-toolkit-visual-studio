using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.PluginServices.Deployment;

using Amazon.AWSToolkit.SimpleWorkers;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for SelectStackPage.xaml
    /// </summary>
    public partial class SelectStackPage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(SelectStackPage));

        string _lastSeenAccount = string.Empty;

        public SelectStackPage()
        {
            InitializeComponent();
            
            DataContext = this;
            this._ctlCreationTimeout.SelectedIndex = 0;
        }

        public SelectStackPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void Initialize(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            this._ctlAccountAndRegionPicker.Initialize(account, region, new[] { DeploymentServiceIdentifiers.CloudFormationServiceName });
            this._ctlAccountAndRegionPicker.PropertyChanged += new PropertyChangedEventHandler(_ctlAccountAndRegionPicker_PropertyChanged);
            UpdateExistingStacks();
            loadTopicList();
        }

        void _ctlAccountAndRegionPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateExistingStacks();
            loadTopicList();

            NotifyPropertyChanged(e.PropertyName);
        }

        public string StackName
        {
            get
            {
                if (this.CreateStackMode)
                    return this._ctlNewStackName.Text;
                else
                    return this._ctlExistingStacks.SelectedValue as string;
            }
        }

        public bool CreateStackMode => this._ctlCreateStack.IsChecked.GetValueOrDefault();

        public AccountViewModel SelectedAccount => this._ctlAccountAndRegionPicker.SelectedAccount;

        public RegionEndPointsManager.RegionEndPoints SelectedRegion => this._ctlAccountAndRegionPicker.SelectedRegion;

        public int CreationTimeout
        {
            get
            {
                int timeout;
                if (!int.TryParse(this._ctlCreationTimeout.SelectedValue.ToString(), out timeout))
                    return -1;

                return timeout;
            }
        }

        public bool RollbackOnFailure => this._ctlRollbackOnFailure.IsChecked.GetValueOrDefault();

        private void loadTopicList()
        {
            if (this._ctlAccountAndRegionPicker.SelectedAccount == null)
            {
                this._ctlSNSTopic.IsEnabled = false;
                return;
            }


            var worker = new QueryTopicArnsWorker(
               this._ctlAccountAndRegionPicker.SelectedAccount.CreateServiceClient<Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient>(this._ctlAccountAndRegionPicker.SelectedRegion),
               LOGGER,
               this.loadTopicListCallback);
        }

        private void loadTopicListCallback(ICollection<string> arns)
        {
            this._ctlSNSTopic.ItemsSource = arns;
        }

        private void UpdateExistingStacks()
        {
            if (this._ctlAccountAndRegionPicker.SelectedAccount == null)
            {
                this._ctlSNSTopic.IsEnabled = false;
                return;
            }

            var worker = new QueryCloudFormationStacksWorker(
                this._ctlAccountAndRegionPicker.SelectedAccount.CreateServiceClient<AmazonCloudFormationClient>(this._ctlAccountAndRegionPicker.SelectedRegion),
                LOGGER,
                this.UpdateExistingStacksCallback);
        }

        void UpdateExistingStacksCallback(ICollection<StackSummary> data)
        {
            var stackNames = data
                .Where(stack => CloudFormationConstants.IsUpdateableStatus(stack.StackStatus))
                .Select(stack => stack.StackName)
                .OrderBy(name => name)
                .ToList();

            this._ctlExistingStacks.ItemsSource = stackNames;

            if (this._ctlExistingStacks.Items.Count != 0)
            {
                this._ctlExistingStacks.SelectedIndex = 0;
            }
        }

        private void onCreateTopicClick(object sender, RoutedEventArgs e)
        {
            if (this._ctlAccountAndRegionPicker.SelectedAccount == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must select an account first before creating a topic.");
                return;
            }

            ISNSRootViewModel model = this._ctlAccountAndRegionPicker.SelectedAccount.FindSingleChild<ISNSRootViewModel>(false);
            ISNSRootViewMetaNode meta = model.MetaNode as ISNSRootViewMetaNode;
            var results = meta.OnCreateTopic(model);

            if (results.Success)
            {
                string topicArn = results.Parameters["CreatedTopic"] as string;
                ICollection<string> arns = this._ctlSNSTopic.ItemsSource as ICollection<string>;
                arns.Add(topicArn);

                this._ctlSNSTopic.Text = topicArn;
                model.AddTopic(this._ctlSNSTopic.Text);
            }
        }

        private void _ctlExistingStacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("StackName");
        }

        private void _ctlCreateStack_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("CreateStackMode");
        }

        private void _ctlNewStackName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized)
            {
                var name = this._ctlNewStackName.Text;
                this._nameValidatedMsg.Visibility = SelectTemplateModel.IsValidStackName(name)
                    ? System.Windows.Visibility.Hidden
                    : System.Windows.Visibility.Visible;
            }

            NotifyPropertyChanged("solutionsStackName");
        }
    }
}
