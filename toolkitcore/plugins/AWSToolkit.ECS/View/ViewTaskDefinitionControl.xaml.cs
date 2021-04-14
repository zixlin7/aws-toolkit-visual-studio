using System;
using System.Windows;
using Amazon.AWSToolkit.ECS.Controller;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewTaskDefinitionControl.xaml
    /// </summary>
    public partial class ViewTaskDefinitionControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewTaskDefinitionControl));

        readonly ViewTaskDefinitionController _controller;

        public ViewTaskDefinitionControl(ViewTaskDefinitionController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title => string.Format("{0} ECS Task Definition", this._controller.RegionDisplayName);

        public override string UniqueId => "Task Definition: " + this._controller.EndPointUniqueIdentifier + "_" + this._controller.Account.Identifier.Id;

        public override bool SupportsBackGroundDataLoad => true;

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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing task definition: " + e.Message);
            }
        }

    }
}
