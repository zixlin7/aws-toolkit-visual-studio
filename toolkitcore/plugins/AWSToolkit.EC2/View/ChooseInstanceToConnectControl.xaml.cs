using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChooseInstanceToConnectControl.xaml
    /// </summary>
    public partial class ChooseInstanceToConnectControl : BaseAWSControl
    {
        public ChooseInstanceToConnectControl()
        {
            InitializeComponent();
        }

        public override string Title => "Choose Instance";
    }
}
