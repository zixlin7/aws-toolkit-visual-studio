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
using Amazon.AWSToolkit.ECS.Controller;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewRepositoryControl.xaml
    /// </summary>
    public partial class ViewRepositoryControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryControl));

        readonly ViewRepositoryController _controller;

        public ViewRepositoryControl(ViewRepositoryController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title
        {
            get
            {
                return string.Format("{0} ECR Repository", this._controller.RegionDisplayName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "Repository: " + this._controller.EndPoint + "_" + this._controller.Account.SettingsUniqueKey;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get
            {
                return true;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                // todo this._controller.RefreshInstances();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository: " + e.Message);
            }
        }

    }
}
