using System;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : BaseAWSControl
    {
        public Browser()
            : this("https://aws.amazon.com/")
        {
            InitializeComponent();
        }

        public Browser(string url)
        {
            InitializeComponent();
            this._ctlBrowser.Navigate(new Uri(url));
        }

        public override string Title => "Getting Started";

        public override string MetricId => this.GetType().FullName;
    }
}
