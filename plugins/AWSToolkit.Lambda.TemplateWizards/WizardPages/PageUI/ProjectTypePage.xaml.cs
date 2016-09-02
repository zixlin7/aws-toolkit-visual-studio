using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageControllers;
using log4net;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageUI
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
            this._ctlAccountAndRegionPicker.Initialize(account, region, new[] { DeploymentServiceIdentifiers.LambdaServiceName });
            this._ctlAccountAndRegionPicker.PropertyChanged += new PropertyChangedEventHandler(_ctlAccountAndRegionPicker_PropertyChanged);

            this._ctlExistingFunctions.SelectionChanged += new SelectionChangedEventHandler(_ctlExistingStacks_SelectionChanged);
            this._ctlSampleFunction.SelectionChanged += new SelectionChangedEventHandler(_ctlSampleTemplate_SelectionChanged);

            this._ctlCreateEmptyProject.Click += new RoutedEventHandler(createMode_Click);
            this._ctlCreateFromFunction.Click += new RoutedEventHandler(createMode_Click);
            this._ctlCreateFromSample.Click += new RoutedEventHandler(createMode_Click);

            UpdateExistingFunctions();
            UpdateSampleFunctions();
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
                if (this._ctlCreateFromFunction.IsChecked.GetValueOrDefault())
                    return ProjectTypeController.CreationMode.ExistingFunction;
                if (this._ctlCreateFromSample.IsChecked.GetValueOrDefault())
                    return ProjectTypeController.CreationMode.FromSample;

                return ProjectTypeController.CreationMode.Empty;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get { return this._ctlAccountAndRegionPicker.SelectedAccount; }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get { return this._ctlAccountAndRegionPicker.SelectedRegion; }
        }

        public string SelectedExistingFunctionName
        {
            get
            {
                return this._ctlExistingFunctions.SelectedValue as string;
            }
        }

        public QueryLambdaFunctionSamplesWorker.SampleSummary SelectedSampleFunction
        {
            get
            {
                return this._ctlSampleFunction.SelectedValue as QueryLambdaFunctionSamplesWorker.SampleSummary;
            }
        }

        void _ctlAccountAndRegionPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateExistingFunctions();

            NotifyPropertyChanged(e.PropertyName);
        }

        private void UpdateExistingFunctions()
        {
            if (this._ctlAccountAndRegionPicker.SelectedAccount != null)
            {
                var client = this._ctlAccountAndRegionPicker.SelectedAccount.CreateServiceClient<AmazonLambdaClient>(this._ctlAccountAndRegionPicker.SelectedRegion);
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
                if (function.Runtime == Amazon.Lambda.Runtime.Nodejs || function.Runtime == Amazon.Lambda.Runtime.Nodejs43)
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

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
