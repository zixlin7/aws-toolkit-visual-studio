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

            this._accountSelector.Initialize();
        }

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
