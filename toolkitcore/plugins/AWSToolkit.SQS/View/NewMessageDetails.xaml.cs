using Amazon.AWSToolkit.CommonUI;

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
            get => this._messageBodyContent;
            set => this._messageBodyContent = value;
        }

        public override string Title => "Send Message";


        public int? DelaySeconds
        {
            get;
            set;
        }
    }
}
