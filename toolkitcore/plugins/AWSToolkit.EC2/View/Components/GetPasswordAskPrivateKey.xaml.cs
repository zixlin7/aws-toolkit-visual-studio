using System.Windows;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Interaction logic for GetPasswordAskPrivateKey.xaml
    /// </summary>
    public partial class GetPasswordAskPrivateKey
    {
        public GetPasswordAskPrivateKey()
        {
            InitializeComponent();
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlPrivateKey.Focus();
        }
    }
}
