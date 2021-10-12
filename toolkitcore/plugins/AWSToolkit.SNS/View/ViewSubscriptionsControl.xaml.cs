using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Controller;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for ViewSubscriptionControl.xaml
    /// </summary>
    public partial class ViewSubscriptionsControl : BaseAWSView
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewSubscriptionsControl));

        ViewSubscriptionsController _controller;
        ViewSubscriptionsModel _model;
        string _title = "";

        public ViewSubscriptionsControl()
            : this(null, null)
        {
        }

        public ViewSubscriptionsControl(ViewSubscriptionsController controller, ViewSubscriptionsModel model)
        {
            this._controller = controller;
            this._model = model;
            InitializeComponent();
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }


        public override string Title => this._title;

        internal void SetTitle(string title)
        {
            this._title = title;
        }

        public override string UniqueId => this.Title;

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordSnsOpenSubscriptions(new SnsOpenSubscriptions()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        #region Toolbar Actions
        private void onCreateSubscriptionClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.CreateSubscription(this._model.OwningTopicARN, null);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating subscription: " + e.Message);
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing subscriptions: " + e.Message);
            }
        }
        #endregion

        #region Context Menu

        private void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            List<SubscriptionEntry> selectedItems = getSelectedItemsAsList();
            if (selectedItems.Count == 0)
                return;

            MenuItem deleteItem = new MenuItem();
            deleteItem.Header = "Delete Subscription";
            deleteItem.Icon = IconHelper.GetIcon(null, "delete.png");
            deleteItem.Click += new RoutedEventHandler(onDeleteRows);

            MenuItem properties = new MenuItem() { Header = "Properties" };
            properties.Click += this.onPropertiesClick;

            

            ContextMenu menu = new ContextMenu();
            menu.Items.Add(deleteItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        private void onDeleteRows(object sender, RoutedEventArgs evnt)
        {
            List<SubscriptionEntry> entries = getSelectedItemsAsList();
            if (entries.Count == 0)
                return;

            try
            {
                string confirmMsg;
                if (entries.Count == 1)
                    confirmMsg = string.Format("Are you sure you want to unsubscribe from \"{0}\"?", entries[0].TopicArn);
                else
                    confirmMsg = string.Format("Are you sure you want to unsubscribe these {0} items?", entries.Count);

                if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Items?", confirmMsg))
                {
                    this._controller.DeleteSubscriptions(entries);
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting subscription: " + e.Message);
            }
        }

        private List<SubscriptionEntry> getSelectedItemsAsList()
        {
            var itemsInClipboard = new List<SubscriptionEntry>();
            foreach (SubscriptionEntry selectedItem in this._ctlDataGrid.SelectedItems)
            {
                itemsInClipboard.Add(selectedItem);
            }

            return itemsInClipboard;
        }
 
        #endregion

        #region Drag and Drop Support

        private void onSubscriptionDragEnter(object sender, DragEventArgs e)
        {
            Type type = typeof(ISQSQueueViewModel);
            if (e.Data.GetDataPresent(type.FullName))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        private void onSubscriptionDrop(object sender, DragEventArgs evnt)
        {
            ISQSQueueViewModel queueViewModel = evnt.Data.GetData("Amazon.AWSToolkit.SQS.Nodes.SQSQueueViewModel") as ISQSQueueViewModel;
            if (queueViewModel == null)
                return;

            try
            {
                this._controller.CreateSubscription(this._model.OwningTopicARN, queueViewModel.QueueARN);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating subscription: " + e.Message);
            }
        }

        #endregion

        void onSelectionChanged(object sender, SelectionChangedEventArgs evnt)
        {
            try
            {
                this.UpdateProperties(DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
            }
        }

        void onPropertiesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this.ShowProperties(DataGridHelper.GetSelectedItems<PropertiesModel>(this._ctlDataGrid));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties", e);
            }
        }
    }
}
