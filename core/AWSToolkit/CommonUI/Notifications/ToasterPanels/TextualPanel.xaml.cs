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
