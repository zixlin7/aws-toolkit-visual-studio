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
using Amazon.AWSToolkit.SQS.Model;

namespace Amazon.AWSToolkit.SQS.View
{
    /// <summary>
    /// Interaction logic for NewMessageDetails.xaml
    /// </summary>
    public partial class NewMessageDetails : BaseAWSControl
    {
        string _messageBodyContent;
        public NewMessageDetails()
        {
            InitializeComponent();
            this._ctlMessageBody.DataContext = this;
            this._ctlDelay.DataContext = this;
        }

        public string MessageBodyContent
        {
            get { return this._messageBodyContent; }
            set { this._messageBodyContent = value; }

        }

        public override string Title
        {
            get { return "Send Message"; }
        }


        public int? DelaySeconds
        {
            get;
            set;
        }
    }
}
