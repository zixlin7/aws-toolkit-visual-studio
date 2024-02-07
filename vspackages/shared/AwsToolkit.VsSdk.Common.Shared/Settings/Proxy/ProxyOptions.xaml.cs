using System.Windows.Controls;

namespace AwsToolkit.VsSdk.Common.Settings.Proxy
{
    /// <summary>
    /// Interaction logic for ProxyOptions.xaml
    /// </summary>
    public partial class ProxyOptions : UserControl
    {
        public ProxyOptions()
        {
            ViewModel = new ProxyOptionsViewModel();
            InitializeComponent();
            DataContext = ViewModel;
        }

        public ProxyOptionsViewModel ViewModel { get; }
    }
}
