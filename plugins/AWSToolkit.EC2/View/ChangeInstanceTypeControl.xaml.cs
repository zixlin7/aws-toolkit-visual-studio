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
    /// Interaction logic for ChangeInstanceTypeControl.xaml
    /// </summary>
    public partial class ChangeInstanceTypeControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ChangeInstanceTypeControl));

        ChangeInstanceTypeController _controller;

        public ChangeInstanceTypeControl(ChangeInstanceTypeController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();

            // switch on grouping for the instance type and vpc subnet dropdowns
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_ctlInstanceTypes.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);
        }

        public override string Title
        {
            get { return "Change Instance Type"; }
        }

        public override bool Validated()
        {
            if (this._controller.Model.SelectedInstanceType == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Instance Type is required.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.ChangeInstanceType();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing instance type", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing instance type: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlInstanceTypes.Focus();
        }
    }
}
