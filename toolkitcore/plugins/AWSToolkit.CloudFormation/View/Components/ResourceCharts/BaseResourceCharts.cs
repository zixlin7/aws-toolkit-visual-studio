using System.Windows.Controls;

namespace Amazon.AWSToolkit.CloudFormation.View.Components.ResourceCharts
{
    public abstract class BaseResourceCharts : UserControl
    {
        public abstract void RenderCharts(int hoursInPast);
    }
}
