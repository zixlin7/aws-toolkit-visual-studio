using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for NodeProjectTypePage.xaml
    /// </summary>
    public partial class NodeProjectTypePage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(NodeProjectTypePage));
        public static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;
        private const int AccountRegionChangedDebounceMs = 250;

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        public NodeProjectTypePage(ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>() {LambdaServiceName});

            InitializeComponent();

            DataContext = this;
            Loaded += OnLoaded;
        }

        public AccountAndRegionPickerViewModel Connection { get; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Unloaded += OnUnloaded;

            _ctlExistingFunctions.SelectionChanged += _ctlExistingStacks_SelectionChanged;
            _ctlSampleFunction.SelectionChanged += _ctlSampleTemplate_SelectionChanged;

            _ctlCreateEmptyProject.Click += createMode_Click;
            _ctlCreateFromFunction.Click += createMode_Click;
            _ctlCreateFromSample.Click += createMode_Click;

            UpdateExistingFunctions();
            UpdateSampleFunctions();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;

            _ctlExistingFunctions.SelectionChanged -= _ctlExistingStacks_SelectionChanged;
            _ctlSampleFunction.SelectionChanged -= _ctlSampleTemplate_SelectionChanged;

            _ctlCreateEmptyProject.Click -= createMode_Click;
            _ctlCreateFromFunction.Click -= createMode_Click;
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

        public NodeProjectTypeController.CreationMode CreationMode
        {
            get
            {
                if (this._ctlCreateFromFunction.IsChecked.GetValueOrDefault())
                    return NodeProjectTypeController.CreationMode.ExistingFunction;
                if (this._ctlCreateFromSample.IsChecked.GetValueOrDefault())
                    return NodeProjectTypeController.CreationMode.FromSample;

                return NodeProjectTypeController.CreationMode.Empty;
            }
        }

        public AccountViewModel SelectedAccount => Connection.Account;

        public ToolkitRegion SelectedRegion => Connection.Region;

        public string SelectedExistingFunctionName => this._ctlExistingFunctions.SelectedValue as string;

        public QueryLambdaFunctionSamplesWorker.SampleSummary SelectedSampleFunction => this._ctlSampleFunction.SelectedValue as QueryLambdaFunctionSamplesWorker.SampleSummary;

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
                    UpdateExistingFunctions();

                    NotifyPropertyChanged(nameof(Connection));
                });
            });
        }

        private void UpdateExistingFunctions()
        {
            if (Connection.ConnectionIsValid)
            {
                var client = Connection.Account.CreateServiceClient<AmazonLambdaClient>(Connection.Region);
                if (client != null)
                {
                    new QueryLambdaFunctionsWorker(client, LOGGER, this.UpdateExistingFunctionsCallback);
                }
            }
        }

        void UpdateExistingFunctionsCallback(ICollection<FunctionConfiguration> data)
        {
            var functionNames = new List<string>();
            foreach (var function in data)
            {
                if (function.Runtime!=null && function.Runtime.Value.StartsWith("nodejs"))
                    functionNames.Add(function.FunctionName);
            }

            this._ctlExistingFunctions.ItemsSource = functionNames.OrderBy(x => x);

            if (this._ctlExistingFunctions.Items.Count != 0)
            {
                this._ctlExistingFunctions.SelectedIndex = 0;
            }
        }

        private void UpdateSampleFunctions()
        {
            var worker = new QueryLambdaFunctionSamplesWorker(
                "us-east-1",
                LOGGER,
                this.UpdateSampleFunctionsCallback);
        }

        void UpdateSampleFunctionsCallback(ICollection<QueryLambdaFunctionSamplesWorker.SampleSummary> data)
        {
            this._ctlSampleFunction.ItemsSource = data;
            if (this._ctlSampleFunction.Items.Count != 0)
            {
                this._ctlSampleFunction.SelectedIndex = 0;
            }
        }
    }
}
