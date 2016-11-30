using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for StackConfigSavedControl.xaml
    /// </summary>
    public partial class StackConfigSavedControl : BaseAWSControl
    {
        public StackConfigSavedControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        public string SuccessFailMsg 
        {
            set { _msg.Text = value; }
        }

        public bool OpenFileForEdit { get; set; }

        public override string Title
        {
            get
            {
                return "Stack Configuration Saved";
            }
        }
    }
}
