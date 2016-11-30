using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
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
