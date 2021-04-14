using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for FastTrackRepublishPage.xaml
    /// </summary>
    public partial class FastTrackRepublishPage : INotifyPropertyChanged
    {
        public FastTrackRepublishPage()
        {
            InitializeComponent();
        }

        public FastTrackRepublishPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void SetRedeploymentMessaging(AccountViewModel account,
                                             ToolkitRegion region,
                                             Stack stack)
        {
            // replace the 'please wait' designer text with specific data about the stack 
            // we're about to deploy to
            _stackDetailsPanel.Children.Clear();

            AddToStackDetailsPanel(string.Format("Republish to AWS CloudFormation Stack '{0}'", stack.StackName));

            AddToStackDetailsPanel(string.Format("This stack was created on {0} and is running in region '{1}'\n(stack ID '{2}').",
                                                 stack.CreationTime.ToLongDateString(),
                                                 region.DisplayName,
                                                 stack.StackId));

            AddToStackDetailsPanel(string.Format("The credentials associated with account '{0}' will be used for the deployment.",
                                                 account.AccountDisplayName));
        }

        void AddToStackDetailsPanel(string text)
        {
            // cannot get the assigned style to give us white-on-dark text from style when dark theme
            // is active, so force the issue by setting an explicit foreground too
            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(4),
                Text = text,
                Style = FindResource("awsTextBlockBaseStyle") as Style,
                Foreground = FindResource("awsDefaultControlForegroundBrushKey") as SolidColorBrush
            };

            _stackDetailsPanel.Children.Add(tb);
        }
    }
}
