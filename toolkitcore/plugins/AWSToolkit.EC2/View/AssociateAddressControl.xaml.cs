using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AssociateAddressControl.xaml
    /// </summary>
    public partial class AssociateAddressControl : BaseAWSControl
    {
        private readonly AssociateAddressModel _model;

        public AssociateAddressControl(AssociateAddressModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = model;
        }

        public override string Title => "Associate Address";

        public override bool Validated()
        {
            if (_model.Instance == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Instance is a required field.");
                return false;
            }

            return true;
        }
    }
}
