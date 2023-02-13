using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeTerminationProtectionControl.xaml
    /// </summary>
    public partial class ChangeTerminationProtectionControl : BaseAWSControl
    {
        public ChangeTerminationProtectionControl(ChangeTerminationProtectionModel model)
        {
            InitializeComponent();
            DataContext = model;
        }

        public override string Title => "Change Termination Protection";
    }
}
