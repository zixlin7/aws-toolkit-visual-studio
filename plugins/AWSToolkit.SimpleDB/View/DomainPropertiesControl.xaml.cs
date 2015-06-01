using System;
using System.Collections.Generic;
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
using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for DomainPropertiesControl.xaml
    /// </summary>
    public partial class DomainPropertiesControl : BaseAWSControl
    {
        DomainPropertiesController _controller;

        public DomainPropertiesControl(DomainPropertiesController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title
        {
            get
            {
                return string.Format("Properties: {0}", this._controller.Model.Domain);
            }
        }
    }
}
