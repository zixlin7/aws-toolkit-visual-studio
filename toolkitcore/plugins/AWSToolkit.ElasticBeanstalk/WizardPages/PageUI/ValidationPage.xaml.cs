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

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ValidationPage.xaml
    /// </summary>
    public partial class ValidationPage : Grid
    {
        public ValidationPage()
        {
            InitializeComponent();
        }

        public string ValidationMessages
        {
            set { _validationMessages.Text = value; }
        }
    }
}
