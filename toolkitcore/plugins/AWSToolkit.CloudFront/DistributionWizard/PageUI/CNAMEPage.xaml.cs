using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI
{
    /// <summary>
    /// Interaction logic for CNAMEPage.xaml
    /// </summary>
    public partial class CNAMEPage
    {
        public CNAMEPage()
        {
            InitializeComponent();
        }

        public CNAMEPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

    }
}
