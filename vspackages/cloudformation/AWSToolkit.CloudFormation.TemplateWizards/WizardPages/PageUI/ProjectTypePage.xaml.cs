using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ProjectTypePage.xaml
    /// </summary>
    public partial class ProjectTypePage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(ProjectTypePage));
        private const int AccountRegionChangedDebounceMs = 250;

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        public AccountAndRegionPickerViewModel Connection { get; }

        public ProjectTypePage(ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>()
            {
                DeploymentServiceIdentifiers.ToolkitCloudFormationServiceName
            });

            InitializeComponent();

            DataContext = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded-= OnLoaded;
            Unloaded += OnUnloaded;

            _ctlExistingStacks.SelectionChanged += _ctlExistingStacks_SelectionChanged;
            _ctlSampleTemplate.SelectionChanged += _ctlSampleTemplate_SelectionChanged;

            _ctlCreateEmptyProject.Click += createMode_Click;
            _ctlCreateFromStack.Click += createMode_Click;
            _ctlCreateFromSample.Click += createMode_Click;

            UpdateSampleTemplates();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;

            _ctlExistingStacks.SelectionChanged -= _ctlExistingStacks_SelectionChanged;
            _ctlSampleTemplate.SelectionChanged -= _ctlSampleTemplate_SelectionChanged;

            _ctlCreateEmptyProject.Click -= createMode_Click;
            _ctlCreateFromStack.Click -= createMode_Click;
            _ctlCreateFromSample.Click -= createMode_Click;
        }

        void createMode_Click(object sender, RoutedEventArgs e)
        {
            this.NotifyPropertyChanged("CreationMode");
        }

        void _ctlSampleTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged("SelectedExistingStackName");
        }

        void _ctlExistingStacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged("SelectedSampleTemplate");
        }

        public ProjectTypeController.CreationMode CreationMode
        {
            get
            {
                if (this._ctlCreateFromStack.IsChecked.GetValueOrDefault())
                    return ProjectTypeController.CreationMode.ExistingStack;
                if (this._ctlCreateFromSample.IsChecked.GetValueOrDefault())
                    return ProjectTypeController.CreationMode.FromSample;

                return ProjectTypeController.CreationMode.Empty;
            }
        }

        public AccountViewModel SelectedAccount => Connection.Account;

        public ToolkitRegion SelectedRegion => Connection.Region;

        public string SelectedExistingStackName => this._ctlExistingStacks.SelectedValue as string;

        public QueryCloudFormationSamplesWorker.SampleSummary SelectedSampleTemplate => this._ctlSampleTemplate.SelectedValue as QueryCloudFormationSamplesWorker.SampleSummary;

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

                    NotifyPropertyChanged(nameof(Connection));
                });
            });
        }

        private void UpdateExistingStacks()
        {
            if (Connection.ConnectionIsValid)
            {
                var worker = new QueryCloudFormationStacksWorker(
                    Connection.Account.CreateServiceClient<AmazonCloudFormationClient>(Connection.Region),
                    LOGGER,
                    this.UpdateExistingStacksCallback);
            }
        }

        void UpdateExistingStacksCallback(ICollection<StackSummary> data)
        {
            var stackNames = new List<string>();
            foreach (var stack in data)
            {
                if (!stack.StackStatus.Value.StartsWith("DELETE"))
                    stackNames.Add(stack.StackName);
            }

            this._ctlExistingStacks.ItemsSource = stackNames;

            if (this._ctlExistingStacks.Items.Count != 0)
            {
                this._ctlExistingStacks.SelectedIndex = 0;
            }
        }

        private void UpdateSampleTemplates()
        {
            var worker = new QueryCloudFormationSamplesWorker(
                "us-east-1",
                LOGGER,
                this.UpdateSampleTemplatesCallback);
        }

        void UpdateSampleTemplatesCallback(ICollection<QueryCloudFormationSamplesWorker.SampleSummary> data)
        {
            this._ctlSampleTemplate.ItemsSource = data;
            if (this._ctlSampleTemplate.Items.Count != 0)
            {
                this._ctlSampleTemplate.SelectedIndex = 0;
            }
        }
    }
}
