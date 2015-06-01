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
