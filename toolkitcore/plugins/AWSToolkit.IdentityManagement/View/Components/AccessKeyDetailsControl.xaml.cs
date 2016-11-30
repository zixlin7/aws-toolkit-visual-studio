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

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.View.Components
{
    /// <summary>
    /// Interaction logic for AccessKeyDetailsControl.xaml
    /// </summary>
    public partial class AccessKeyDetailsControl : BaseAWSControl
    {
        public AccessKeyDetailsControl()
        {
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Access Keys";
            }
        }
    }
}
