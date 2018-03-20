using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using log4net;

namespace Amazon.AWSToolkit.Lambda.View
{
    /// <summary>
    /// Interaction logic for ConfirmRoleCleanupControl.xaml
    /// </summary>
    public partial class ConfirmRoleCleanupControl : BaseAWSControl
    {
        private string _roleName;

        public ConfirmRoleCleanupControl()
        {
            InitializeComponent();
        }

        public ConfirmRoleCleanupControl(string roleName, string currentTrustPolicy, string newTrustPolicy)
            : this()
        {
            this._roleName = roleName;

            this._ctlDescription.Text = this._ctlDescription.Text.Replace("The selected role", string.Format("The selected role {0}", this._roleName));

            this._ctlExistingTrustPolicy.Text = Utility.PrettyPrintJson(currentTrustPolicy);
            this._ctlNewTrustPolicy.Text = newTrustPolicy;
        }

        public override string Title
        {
            get { return string.Format("Fix Trust Policy for role {0}?", this._roleName); }
        }
    }
}
