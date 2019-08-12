using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI
{
    /// <summary>
    /// Interaction logic for LoggingPage.xaml
    /// </summary>
    public partial class LoggingPage 
    {
        public LoggingPage()
        {
            InitializeComponent();
        }

        public LoggingPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._ctlLogging.Initialize(controller);
        }

        public IAWSWizardPageController PageController { get; set; }
    }
}
