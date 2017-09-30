using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PublishProgressPage.xaml
    /// </summary>
    public partial class PublishProgressPage : BaseAWSUserControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PublishProgressPage));

        public PublishProgressPageController PageController { get; private set; }

        public PublishProgressPage()
        {
            InitializeComponent();
        }

        public PublishProgressPage(PublishProgressPageController pageController)
            : this()
        {
            PageController = pageController;
        }

        public void SetUploadFailedState(bool failed)
        {
            if (failed)
            {
                this._ctlProgressBar.Visibility = Visibility.Hidden;
                this._ctlUploadFailedMessage.Visibility = Visibility.Visible;
            }
            else
            {
                this._ctlProgressBar.Visibility = Visibility.Visible;
                this._ctlUploadFailedMessage.Visibility = Visibility.Hidden;
            }
        }
    }
}
