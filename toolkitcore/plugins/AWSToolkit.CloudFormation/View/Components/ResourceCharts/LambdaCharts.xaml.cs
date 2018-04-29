using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.View.Components.ResourceCharts
{
    /// <summary>
    /// Interaction logic for LambdaCharts.xaml
    /// </summary>
    public partial class LambdaCharts : BaseResourceCharts
    {
        IAmazonCloudWatch _cwClient;
        string _functionName;
        public LambdaCharts(IAmazonCloudWatch cwClient, string functionName)
        {
            this._cwClient = cwClient;
            this._functionName = functionName;
            InitializeComponent();

            this._ctlLabel.Text = "Metrics for Lambda Function " + this._functionName;
        }

        public override void RenderCharts(int hoursInPast)
        {
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension() { Name = "FunctionName", Value = this._functionName });

            this._ctlInvocations.Render(this._cwClient, "AWS/Lambda", "Invocations", "Sum", "Count", dimensions, hoursInPast);
            this._ctlDurations.Render(this._cwClient, "AWS/Lambda", "Duration", "Average", "Milliseconds", dimensions, hoursInPast);

            this._ctlErrors.Render(this._cwClient, "AWS/Lambda", "Errors", "Sum", "Count", dimensions, hoursInPast);
            this._ctlThrottles.Render(this._cwClient, "AWS/Lambda", "Throttles", "Sum", "Count", dimensions, hoursInPast);
        }
    }
}
