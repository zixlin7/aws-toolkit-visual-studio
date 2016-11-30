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

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : BaseAWSControl
    {
        public Browser()
            : this("http://aws.amazon.com/")
        {
            InitializeComponent();
        }

        public Browser(string url)
        {
            InitializeComponent();
            this._ctlBrowser.Navigate(new Uri(url));
        }

        public override string Title
        {
            get
            {
                return "Getting Started";
            }
        }

        public override string MetricId
        {
            get { return this.GetType().FullName; }
        }
    }
}
