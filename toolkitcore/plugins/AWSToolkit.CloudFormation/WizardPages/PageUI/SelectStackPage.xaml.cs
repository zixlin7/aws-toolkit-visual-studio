using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.Util;
using Amazon.SimpleNotificationService;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for SelectStackPage.xaml
    /// </summary>
    public partial class SelectStackPage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(SelectStackPage));
        private const int AccountRegionChangedDebounceMs = 250;

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        public AccountAndRegionPickerViewModel Connection { get; }

        public SelectStackPage(ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>()
            {
                DeploymentServiceIdentifiers.ToolkitCloudFormationServiceName
            });

            InitializeComponent();

            DataContext = this;
            this._ctlCreationTimeout.SelectedIndex = 0;
        }

        void ConnectionChanged(object sender, EventArgs e)
        {
            if (!Connection.ConnectionIsValid)
            {
                return;
            }

            // Prevent multiple loads caused by property changed events in rapid succession
            _accountRegionChangeDebounceDispatcher.Debounce(AccountRegionChangedDebounceMs, _ =>
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                {
                    UpdateExistingStacks();
                    loadTopicList();

                    NotifyPropertyChanged(nameof(Connection));
                });
            });
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
            if (!Connection.ConnectionIsValid)
            {
                this._ctlSNSTopic.IsEnabled = false;
                return;
            }


            var worker = new QueryTopicArnsWorker(
               Connection.Account.CreateServiceClient<AmazonSimpleNotificationServiceClient>(Connection.Region),
               LOGGER,
               this.loadTopicListCallback);
        }

        private void loadTopicListCallback(ICollection<string> arns)
        {
            this._ctlSNSTopic.ItemsSource = arns;
        }

        private void UpdateExistingStacks()
        {
            if (!Connection.ConnectionIsValid)
            {
                this._ctlSNSTopic.IsEnabled = false;
                return;
            }

            var worker = new QueryCloudFormationStacksWorker(
                Connection.Account.CreateServiceClient<AmazonCloudFormationClient>(Connection.Region),
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
            if (!Connection.ConnectionIsValid)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Valid AWS Credentials are required to create a SNS topic.");
                return;
            }

            ISNSRootViewModel model = Connection.Account.FindSingleChild<ISNSRootViewModel>(false);
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
