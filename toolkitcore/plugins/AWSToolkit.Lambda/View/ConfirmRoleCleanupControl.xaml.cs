using Amazon.AWSToolkit.CommonUI;

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

        public override string Title => string.Format("Fix Trust Policy for role {0}?", this._roleName);
    }
}
