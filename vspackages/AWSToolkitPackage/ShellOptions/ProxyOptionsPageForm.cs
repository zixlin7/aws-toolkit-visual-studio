using System.Windows.Forms;

namespace Amazon.AWSToolkit.VisualStudio.ShellOptions
{
    public partial class ProxyOptionsPageForm : UserControl
    {
        public ProxyOptionsPageForm()
        {
            InitializeComponent();
        }

        public ProxyUtilities.ProxySettings ProxySettings
        {
            get
            {
                var settings = new ProxyUtilities.ProxySettings()
                {
                    Host = this._ctlHost.Text,
                    Username = this._ctlUsername.Text,
                    Password = this._ctlPassword.Text
                };

                int port;
                if (int.TryParse(this._ctlPort.Text, out port))
                    settings.Port = port;

                return settings;
            }
            set
            {
                this._ctlHost.Text = value.Host;
                if (value.Port.HasValue)
                    this._ctlPort.Text = value.Port.Value.ToString();
                this._ctlUsername.Text = value.Username;
                this._ctlPassword.Text = value.Password;
            }
        }
    }
}
