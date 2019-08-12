using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI
{
    /// <summary>
    /// Interaction logic for PrivateSettingsPage.xaml
    /// </summary>
    public partial class PrivateSettingsPage
    {
        public PrivateSettingsPage()
        {
            InitializeComponent();
        }

        public PrivateSettingsPage(PrivateSettingsPageController controller)
            : this()
        {
            this.PageController = controller;
            this._ctlTrustedSigners.Initialize(controller.EditorController);
        }

        public IAWSWizardPageController PageController { get; set; }

    }
}
