using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for GetConsoleControl.xaml
    /// </summary>
    public partial class GetConsoleOutputControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(GetConsoleOutputControl));

        GetConsoleOutputController _controller;

        public GetConsoleOutputControl(GetConsoleOutputController controller)
        {
            this._controller = controller;
            this.DataContext = controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return string.Format("Console Output for Instance {0}", this._controller.Model.InstanceId);
            }
        }
    }
}
