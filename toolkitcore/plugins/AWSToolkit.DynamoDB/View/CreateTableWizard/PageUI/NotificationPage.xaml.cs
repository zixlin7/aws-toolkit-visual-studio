using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI
{
    /// <summary>
    /// Interaction logic for NotificationPage.xaml
    /// </summary>
    public partial class NotificationPage
    {
        NotificationPageController _controller;

        public NotificationPage(NotificationPageController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.DataContext;
        }

        private void aboutDynamoDB_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("http://aws.amazon.com/dynamodb/#pricing"));
            e.Handled = true;
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            this._controller.TestForwardTransitionEnablement();
        }

        private void onClick(object sender, RoutedEventArgs e)
        {
            this._controller.TestForwardTransitionEnablement();
        }
    }
}
