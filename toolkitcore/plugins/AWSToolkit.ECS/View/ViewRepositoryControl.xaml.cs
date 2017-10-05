using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Nodes;
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
            this._controller = controller;
            DataContext = _controller.Model;

            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                if (this._controller.Model.Repository == null)
                    return "Repository";

                return string.Format("Repository {0}", this._controller.Model.Repository.Name);
            }
        }

        public override string UniqueId
        {
            get
            {
                return string.Format("Repository: {0} {1}_{2}",
                                    (this._controller.FeatureViewModel as RepositoryViewModel).Repository.RepositoryArn,
                                     this._controller.EndPoint,
                                     this._controller.Account.SettingsUniqueKey);
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

        void onLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.LoadModel();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository: " + e.Message);
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            
        }
    }
}
