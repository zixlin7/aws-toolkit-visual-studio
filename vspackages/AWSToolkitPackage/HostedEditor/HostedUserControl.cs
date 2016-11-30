using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using Amazon.AWSToolkit.CommonUI;
using System.Windows;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.VisualStudio.HostedEditor
{
    public partial class HostedUserControl : UserControl
    {
        IAWSToolkitControl _hostedControl;

        public HostedUserControl()
        {
            InitializeComponent();
        }

        public void AddHostedControl(IAWSToolkitControl hostedControl)
        {
            this._hostedControl = hostedControl;

            ThemeUtil.UpdateDictionariesForTheme(this._hostedControl.UserControl.Resources);

            var elementHost = new ElementHost
                {
                    Dock = DockStyle.Fill, 
                    Child = this._hostedControl.UserControl
                };

            this.Controls.Add(elementHost);
        }
    }
}
