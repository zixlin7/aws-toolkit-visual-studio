using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.WizardPages.PageControllers;
using log4net;
using System;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for UploadFunctionProgressPage.xaml
    /// </summary>
    public partial class UploadFunctionProgressPage : BaseAWSUserControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionProgressPage));

        public UploadFunctionProgressPageController PageController { get; }

        //indicates if page is manually closed/unloaded by user
        public bool IsUnloaded { get; set; }

        public UploadFunctionProgressPage()
        {
            InitializeComponent();
        }

        public UploadFunctionProgressPage(UploadFunctionProgressPageController pageController)
            : this()
        {
            PageController = pageController;

            var originator = (UploadFunctionController.UploadOriginator)pageController.HostingWizard.CollectedProperties[UploadFunctionWizardProperties.UploadOriginator];
            if (originator == UploadFunctionController.UploadOriginator.FromFunctionView)
                this._ctlOpenView.Visibility = Visibility.Hidden;

            if(pageController.PublishMode == UploadFunctionProgressPageController.Mode.Serverless)
            {
                _ctlOpenView.Content = "Open CloudFormation Stack view after publish initiated.";
            }
            else
            {
                _ctlOpenView.Content = "Open Lambda Function view after upload complete.";
            }
            this.Unloaded += OnUnloaded;
        }

        public bool OpenView
        {
            get
            {
                if (this._ctlOpenView.Visibility != Visibility.Visible)
                    return false;

                return this._ctlOpenView.IsChecked.GetValueOrDefault();
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

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            IsUnloaded = true;
            this.Unloaded -= this.OnUnloaded;
        }
    }
}
