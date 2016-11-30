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


using Amazon.SQS.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SQS.Model;

namespace Amazon.AWSToolkit.SQS.View
{
    /// <summary>
    /// Interaction logic for ExamineMessageControl.xaml
    /// </summary>
    public partial class ExamineMessageControl : BaseAWSControl
    {
        MessageWrapper _message;

        public ExamineMessageControl()
            : this(new MessageWrapper(new Message()))
        {
        }

        public ExamineMessageControl(MessageWrapper message)
        {
            this._message = message;
            this.DataContext = this._message;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Message Body";
            }
        }
    }
}
