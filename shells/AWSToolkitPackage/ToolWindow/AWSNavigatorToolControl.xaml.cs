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

using Amazon.AWSToolkit.Navigator;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.ToolWindow
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class AWSNavigatorToolControl
    {
        public AWSNavigatorToolControl()
        {
            InitializeComponent();

            ThemeUtil.UpdateDictionariesForTheme(this.Resources);

            this._navigatorHost.Children.Add(ToolkitFactory.Instance.Navigator);
        }
    }
}