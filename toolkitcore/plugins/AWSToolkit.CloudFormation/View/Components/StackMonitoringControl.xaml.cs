using System;
using System.Linq;
using System.Windows.Controls;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.View.Components.ResourceCharts;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for StackMonitoringControl.xaml
    /// </summary>
    public partial class StackMonitoringControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StackMonitoringControl));

        ViewStackController _controller;

        public StackMonitoringControl()
        {
            InitializeComponent();
            this._ctlPeriodPicker.ItemsSource = CloudWatchDataFetcher.MonitorPeriod.Periods;
            this._ctlPeriodPicker.SelectedItem = CloudWatchDataFetcher.MonitorPeriod.Periods.ToArray()[0];
        }

        public void Initialize(ViewStackController controller)
        {
            this._controller = controller;
        }

        private void onPeriodChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't load data an initial selection of combo box
            if (e.RemovedItems.Count == 0)
                return;

            LoadCloudWatchData();
        }

        public void LoadCloudWatchData()
        {
            try
            {
                var period = this._ctlPeriodPicker.SelectedItem as CloudWatchDataFetcher.MonitorPeriod;
                if (period == null)
                    return;

                this._ctlMainPanel.Children.Clear();
                var resources = this._controller.GetStackResources();
                if (resources == null)
                    return;

                foreach (var resource in resources)
                {
                    if (resource.ResourceStatus != CloudFormationConstants.CreateCompleteStatus)
                        continue;

                    BaseResourceCharts charts = null;
                    switch (resource.ResourceType)
                    {
                        case "AWS::ElasticLoadBalancing::LoadBalancer":
                            charts = new LoadBalancerCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                            break;
                        case "AWS::AutoScaling::AutoScalingGroup":
                            charts = new AutoScalingCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                            break;
                        case "AWS::EC2::Instance":
                            charts = new InstanceCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                            break;
                        case "AWS::RDS::DBInstance":
                            charts = new RDSInstanceCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                            break;
                        case "AWS::Lambda::Function":
                            charts = new LambdaCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                            break;

                        // TODO: We need to convert the API Gateway ID to the API Gateway Name
                        //case "AWS::ApiGateway::RestApi":
                        //    charts = new APIGatewayCharts(this._controller.CloudWatchClient, resource.PhysicalResourceId);
                        //    break;
                    }

                    if (charts != null)
                    {
                        charts.RenderCharts(period.HoursInPast);
                        this._ctlMainPanel.Children.Add(charts);
                    }
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading cloud watch data for stack resources: " + e.Message);
                LOGGER.Error("Error loading cloud watch data for stack resources", e);
            }
        }
    }
}
