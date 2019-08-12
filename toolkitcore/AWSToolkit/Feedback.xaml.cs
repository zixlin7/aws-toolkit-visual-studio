using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Interaction logic for Feedback.xaml
    /// </summary>
    public partial class Feedback : BaseAWSControl
    {
        public Feedback()
        {
            InitializeComponent();
        }

        public override string Title => "Send AWS Toolkit for Visual Studio Feedback";

        public override string MetricId => this.GetType().FullName;
    }
}
