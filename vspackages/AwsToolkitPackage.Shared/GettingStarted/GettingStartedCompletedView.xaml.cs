using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    /// <summary>
    /// Interaction logic for GettingStartedCompletedView.xaml
    /// </summary>
    public partial class GettingStartedCompletedView : UserControl
    {
        public GettingStartedCompletedView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Prevents any click events in the TextBlock from reaching the parent CheckBox
        /// </summary>
        private void TextBlock_SwallowEvents(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
