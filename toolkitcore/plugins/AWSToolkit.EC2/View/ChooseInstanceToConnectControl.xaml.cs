using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
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

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChooseInstanceToConnectControl.xaml
    /// </summary>
    public partial class ChooseInstanceToConnectControl : BaseAWSControl
    {
        public ChooseInstanceToConnectControl()
        {
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Choose Instance";
            }
        }
    }
}
