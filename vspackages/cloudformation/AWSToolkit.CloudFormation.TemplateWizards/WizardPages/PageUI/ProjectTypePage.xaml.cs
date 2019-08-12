using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ProjectTypePage.xaml
    /// </summary>
    public partial class ProjectTypePage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(ProjectTypePage));

        public ProjectTypePage()
        {
            InitializeComponent();

            DataContext = this;
        }

        public ProjectTypePage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void Initialize(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            this._ctlAccountAndRegionPicker.Initialize(account, region, new[] { DeploymentServiceIdentifiers.CloudFormationServiceName });
            this._ctlAccountAndRegionPicker.PropertyChanged += new PropertyChangedEventHandler(_ctlAccountAndRegionPicker_PropertyChanged);

            this._ctlExistingStacks.SelectionChanged += new SelectionChangedEventHandler(_ctlExistingStacks_SelectionChanged);
            this._ctlSampleTemplate.SelectionChanged += new SelectionChangedEventHandler(_ctlSampleTemplate_SelectionChanged);

            this._ctlCreateEmptyProject.Click += new RoutedEventHandler(createMode_Click);
            this._ctlCreateFromStack.Click += new RoutedEventHandler(createMode_Click);
            this._ctlCreateFromSample.Click += new RoutedEventHandler(createMode_Click);

            UpdateExistingStacks();
            UpdateSampleTemplates();
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

        public AccountViewModel SelectedAccount => this._ctlAccountAndRegionPicker.SelectedAccount;

        public RegionEndPointsManager.RegionEndPoints SelectedRegion => this._ctlAccountAndRegionPicker.SelectedRegion;

        public string SelectedExistingStackName => this._ctlExistingStacks.SelectedValue as string;

        public QueryCloudFormationSamplesWorker.SampleSummary SelectedSampleTemplate => this._ctlSampleTemplate.SelectedValue as QueryCloudFormationSamplesWorker.SampleSummary;

        void _ctlAccountAndRegionPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateExistingStacks();

            NotifyPropertyChanged(e.PropertyName);
        }

        private void UpdateExistingStacks()
        {
            if (this._ctlAccountAndRegionPicker.SelectedAccount != null)
            {
                var worker = new QueryCloudFormationStacksWorker(
                    this._ctlAccountAndRegionPicker.SelectedAccount.CreateServiceClient<AmazonCloudFormationClient>(this._ctlAccountAndRegionPicker.SelectedRegion),
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
