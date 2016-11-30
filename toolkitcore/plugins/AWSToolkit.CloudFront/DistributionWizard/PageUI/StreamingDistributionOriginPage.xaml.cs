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

using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI
{
    /// <summary>
    /// Interaction logic for StreamingDistributionOriginPage.xaml
    /// </summary>
    public partial class StreamingDistributionOriginPage
    {
        public StreamingDistributionOriginPage()
        {
            InitializeComponent();
        }

        public StreamingDistributionOriginPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._ctlS3Origin.Initialize(controller);
        }

        public IAWSWizardPageController PageController { get; set; }
    }
}
