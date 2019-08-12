using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.View.Components
{
    /// <summary>
    /// Interaction logic for AccessKeyDetailsControl.xaml
    /// </summary>
    public partial class AccessKeyDetailsControl : BaseAWSControl
    {
        public AccessKeyDetailsControl()
        {
            InitializeComponent();
        }

        public override string Title => "Access Keys";
    }
}
