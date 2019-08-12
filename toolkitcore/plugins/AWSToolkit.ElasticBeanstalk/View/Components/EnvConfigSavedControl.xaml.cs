using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for EnvConfigSavedControl.xaml
    /// </summary>
    public partial class EnvConfigSavedControl : BaseAWSControl
    {
        public EnvConfigSavedControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        public string SuccessFailMsg 
        {
            set => _msg.Text = value;
        }

        public bool OpenFileForEdit { get; set; }

        public override string Title => "Environment Configuration Saved";
    }
}
