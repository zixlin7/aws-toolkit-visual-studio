using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for CNAMEControl.xaml
    /// </summary>
    public partial class CNAMEControl
    {
        public CNAMEControl()
        {
            InitializeComponent();
        }

        protected void OnAddCNAME(object sender, RoutedEventArgs e)
        {
            BaseConfigModel model = this.DataContext as BaseConfigModel;
            model.CNAMEs.Add(new MutableString());
            this._ctlCNAMEs.SelectedIndex = model.CNAMEs.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlCNAMEs, this._ctlCNAMEs.SelectedIndex, 0);
        }

        protected void OnRemoveCNAME(object sender, RoutedEventArgs e)
        {
            BaseConfigModel model = this.DataContext as BaseConfigModel;
            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in this._ctlCNAMEs.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (MutableString value in itemsToBeRemoved)
            {
                model.CNAMEs.Remove(value);
            }
        }

        void onRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url = "http://docs.amazonwebservices.com/AmazonCloudFront/latest/DeveloperGuide/CNAMEs.html";
            Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }
    }
}
