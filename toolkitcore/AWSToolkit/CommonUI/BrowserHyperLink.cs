using System;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Navigation;

using log4net;

namespace Amazon.AWSToolkit.CommonUI
{
    public class BrowserHyperLink : Hyperlink
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BrowserHyperLink));

        public BrowserHyperLink()
        {
            RequestNavigate += OnRequestNavigate;
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
                Logger.Error(e);
            }
        }
    }
}
