using System;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    /// <summary>
    /// Interaction logic for GettingStartedView.xaml
    /// </summary>
    public partial class GettingStartedView : BaseAWSControl, IDisposable
    {
        public override string Title => "AWS Getting Started";

        public override string UniqueId => "AWSGettingStarted";

        public override bool IsUniquePerAccountAndRegion => false;

        public GettingStartedView()
        {
            InitializeComponent();

            Unloaded += GettingStartedView_Unloaded;
        }

        private void GettingStartedView_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= GettingStartedView_Unloaded;

            Dispose();
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                (DataContext as IDisposable)?.Dispose();
                DataContext = null;
            }
        }
    }
}
