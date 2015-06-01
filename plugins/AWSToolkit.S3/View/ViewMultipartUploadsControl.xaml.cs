using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

using log4net;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for ViewMultipartUploadsControl.xaml
    /// </summary>
    public partial class ViewMultipartUploadsControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewMultipartUploadsController));
        ViewMultipartUploadsController _controller;

        public ViewMultipartUploadsControl(ViewMultipartUploadsController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return string.Format("Multiparts: {0}", this._controller.Model.BucketName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return "multipart." + this._controller.Model.BucketName;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this._controller.Refresh();
        }

        void onAbortClick(object sender, RoutedEventArgs e)
        {
            if (this._ctlMultiparts.SelectedItems.Count == 0)
                return;

            try
            {
                var selectedItems = new List<ViewMultipartUploadsModel.MultipartUploadWrapper>();
                foreach (ViewMultipartUploadsModel.MultipartUploadWrapper item in this._ctlMultiparts.SelectedItems)
                    selectedItems.Add(item);

                string confirmMsg;
                if (selectedItems.Count == 1)
                    confirmMsg = string.Format("Are you sure you want to abort \"{0}\"?", selectedItems[0].Key);
                else
                    confirmMsg = string.Format("Are you sure you want to abort {0} uploads?", selectedItems.Count);

                if(ToolkitFactory.Instance.ShellProvider.Confirm("Abort Upload", confirmMsg))
                    this._controller.AbortUploads(selectedItems);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error aborting upload", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error aborting upload: " + ex.Message);
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            if (this._ctlMultiparts.SelectedItems.Count == 0)
                return;

            ContextMenu menu = new ContextMenu();

            if (this._ctlMultiparts.SelectedItems.Count == 1)
            {
                MenuItem viewParts = new MenuItem();
                viewParts.Header = "View Parts";
                viewParts.Icon = IconHelper.GetIcon("S3.view-parts.png");
                viewParts.Click += new RoutedEventHandler(onViewParts);

                menu.Items.Add(viewParts);
            }
            MenuItem abortItem = new MenuItem();
            abortItem.Header = "Abort";
            abortItem.Icon = IconHelper.GetIcon("abort.png");
            abortItem.Click += new RoutedEventHandler(onAbortClick);
            menu.Items.Add(abortItem);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        void onViewParts(object sender, RoutedEventArgs args)
        {
            try
            {
                var upload = this._ctlMultiparts.SelectedItem as ViewMultipartUploadsModel.MultipartUploadWrapper;
                if (upload == null)
                    return;
                this._controller.DisplayParts(upload);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error displaying parts", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error displaying parts: " + ex.Message);
            }
        }

        void onRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            onViewParts(sender, new RoutedEventArgs());
        }


        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }
    }
}
