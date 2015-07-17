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

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Interaction logic for Feedback.xaml
    /// </summary>
    public partial class Feedback : BaseAWSControl
    {
        public Feedback()
        {
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Send AWS Toolkit for Visual Studio Feedback";
            }
        }

        public override string MetricId
        {
            get { return this.GetType().FullName; }
        }
    }
}
