using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for RestoreObjectPromptControl.xaml
    /// </summary>
    public partial class RestoreObjectPromptControl : BaseAWSControl
    {
        RestoreObjectPromptController _controller;

        public RestoreObjectPromptControl(RestoreObjectPromptController controller)
        {
            InitializeComponent();

            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get
            {
                return "Initiate Restore";
            }
        }

        public override bool Validated()
        {
            int days;
            if (!int.TryParse(this._controller.Model.RestoreDays, out days))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Days is a required field.");
                return false;
            }

            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlDaysToRestore.Focus();
        }

        void _days_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }
    }
}
