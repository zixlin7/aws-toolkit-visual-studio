using System.Windows;
using Amazon.AWSToolkit.CommonUI;
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

        public PublishProgressPageController PageController { get; }

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

        public bool AutoCloseWizard => this._ctlAutoCloseWizard.IsChecked.GetValueOrDefault();

        public void OutputProgressMessage(string message)
        {
            this._ctlProgressMessages.Text += string.Concat(message, Environment.NewLine);
            this._ctlProgressMessages.ScrollToEnd();
        }

        public void StartProgressBar()
        {
            _ctlProgressBar.IsIndeterminate = true;
        }

        public void StopProgressBar()
        {
            _ctlProgressBar.IsIndeterminate = false;
            _ctlProgressBar.Value = _ctlProgressBar.Maximum;
        }
    }
}
