using System;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Navigation;

using log4net;

namespace Amazon.AWSToolkit.CommonUI
{
    public class BrowserHyperLink : Hyperlink
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(BrowserHyperLink));

        public BrowserHyperLink()
        {
            RequestNavigate += OnRequestNavigate;
        }

        public string NavigateUriFromStatic
        {
            get => NavigateUri.OriginalString;
            set => NavigateUri = new Uri(value);
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenInBrowser(e.Uri);
            e.Handled = true;
        }

        private void OpenInBrowser(Uri uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri));
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
    }
}
