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

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for EnvConfigSavedControl.xaml
    /// </summary>
    public partial class EnvConfigSavedControl : BaseAWSControl
    {
        public EnvConfigSavedControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        public string SuccessFailMsg 
        {
            set { _msg.Text = value; }
        }

        public bool OpenFileForEdit { get; set; }

        public override string Title
        {
            get
            {
                return "Environment Configuration Saved";
            }
        }
    }
}
