using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.Notifications.ToasterPanels
{
    /// <summary>
    /// Simple header/message text panel for use with AWSNotificationToaster
    /// </summary>
    public partial class TextualPanel : Grid
    {
        public TextualPanel()
        {
            InitializeComponent();
        }

        public string MessageText
        {
            set 
            { 
                this._message.Text = value;
                this._message.ToolTip = value;
            }
        }
    }
}
