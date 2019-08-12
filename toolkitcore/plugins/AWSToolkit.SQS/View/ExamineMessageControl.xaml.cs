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

        public override string Title => "Message Body";
    }
}
