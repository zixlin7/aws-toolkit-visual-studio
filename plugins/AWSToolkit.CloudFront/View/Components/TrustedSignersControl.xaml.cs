using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for TrustedSignersControl.xaml
    /// </summary>
    public partial class TrustedSignersControl
    {
        BaseDistributionConfigEditorController _controller;
        public TrustedSignersControl()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
        }

        protected void OnAddTrustedSigner(object sender, RoutedEventArgs e)
        {
            BaseConfigModel model = this.DataContext as BaseConfigModel;
            model.TrustedSignerAWSAccountIds.Add(new MutableString());
            this._ctlTrustedSignerAccountIds.SelectedIndex = model.TrustedSignerAWSAccountIds.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlTrustedSignerAccountIds, this._ctlTrustedSignerAccountIds.SelectedIndex, 0);
        }

        protected void OnRemoveTrustedSigner(object sender, RoutedEventArgs e)
        {
            BaseConfigModel model = this.DataContext as BaseConfigModel;
            List<MutableString> itemsToBeRemoved = new List<MutableString>();
            foreach (MutableString value in this._ctlTrustedSignerAccountIds.SelectedItems)
            {
                itemsToBeRemoved.Add(value);                
            }

            foreach (MutableString value in itemsToBeRemoved)
            {
                model.TrustedSignerAWSAccountIds.Remove(value);
            }
        }

        protected void onCreateOriginAccessIdentityClick(object sender, RoutedEventArgs e)
        {
            this._controller.CreateOriginAccessIdentity();
        }

        void onRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url = "http://docs.amazonwebservices.com/AmazonCloudFront/latest/DeveloperGuide/PrivateContentOverview.html";
            Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }
    }
}
