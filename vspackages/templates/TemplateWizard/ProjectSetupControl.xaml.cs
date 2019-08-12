using Amazon.AWSToolkit.CommonUI;

namespace TemplateWizard
{
    /// <summary>
    /// Interaction logic for ProjectSetupControl.xaml
    /// </summary>
    public partial class ProjectSetupControl : BaseAWSControl
    {
        public ProjectSetupControl()
        {
            InitializeComponent();
            this.Height = 200;
            this._accountSelector.SwitchToVerticalLayout();
            this._accountSelector.Initialize();
        }

        public override string Title => "AWS Access Credentials";

        public override bool OnCommit()
        {
            this.AccountName = this._accountSelector.SelectedAccount?.DisplayName;
            this.RegionName = this._accountSelector.SelectedRegion?.SystemName;
            return base.OnCommit();
        }

        public string AccountName { get; set; }
        public string RegionName { get; set; }
    }
}
