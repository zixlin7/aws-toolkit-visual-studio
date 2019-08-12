using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using log4net;

using Microsoft.Win32;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.JobTracker;
using Amazon.AWSToolkit.S3.Clipboard;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Jobs;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for BucketBrowserControl.xaml
    /// </summary>
    public partial class BucketBrowserControl : BaseAWSControl
    {
        ILog _logger = LogManager.GetLogger(typeof(BucketBrowserControl));

        BucketBrowserController _controller;
        BucketBrowserModel _model;

        public BucketBrowserControl()
            : this(null)
        {
        }

        public BucketBrowserControl(BucketBrowserController controller)
            : this(controller, new BucketBrowserModel(string.Empty))
        {
        }

        public BucketBrowserControl(BucketBrowserController controller, BucketBrowserModel model)
        {
            this._controller = controller;
            this._model = model;

            this._model.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onModelPropertyChanged);
            this._model.NewItems += new EventHandler(onNewChildItems);
            InitializeComponent();

            this.buildBreadCrumb();
            this._ctlDataGrid.SelectedItem = null;

            this.Unloaded += new RoutedEventHandler(onUnloaded);
        }

        void onUnloaded(object sender, RoutedEventArgs e)
        {
            this._controller.DisposeLoadingThread();
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.StartFetchingObject();            
            return this._controller.Model;
        }

        public BucketBrowserModel Model => this.DataContext as BucketBrowserModel;

        public override string Title => "S3 Bucket: " + this._controller.BucketName;

        public override string UniqueId => string.Format("S3:BucketBrowser:{0}", this._controller.BucketName);

        private void RowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // github.com/aws/aws-sdk-net/issues/160, double click on scroll bar
            // was opening the object. Note that the row header image does not report as
            // having a parent
            var frameworkElement = e.MouseDevice.DirectlyOver as FrameworkElement;
            if (frameworkElement == null)
                return;

            if (frameworkElement.Parent is DataGridCell || (frameworkElement.Parent == null && frameworkElement is System.Windows.Controls.Image))
            {
                var childItem = this._ctlDataGrid.SelectedItem as BucketBrowserModel.ChildItem;
                if (childItem == null)
                    return;

                if (childItem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
                {
                    navigateUpDirectory();
                }
                else if (childItem.ChildType == BucketBrowserModel.ChildType.Folder)
                {
                    this.Model.Path = childItem.FullPath;
                }
                else if (childItem.ChildType == BucketBrowserModel.ChildType.File)
                {
                    this._controller.Open(childItem.FullPath);
                }
            }
        }

        public void SetDataGridFocus()
        {
            if (this._ctlDataGrid.Items.Count == 0)
            {
                return;
            }

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this._ctlDataGrid.Focus();
                var cell = DataGridHelper.GetCell(this._ctlDataGrid, 0, 0);
                if (cell != null)
                {
                    cell.Focus();
                }
            }));
        }


        #region Bread Crumb Operations

        void onModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Path"))
            {
                buildBreadCrumb();
            }
        }


        private void buildBreadCrumb()
        {
            try
            {
                // Clear out existing breadcrumb
                this._ctlBreadCrumbPanel.Children.RemoveRange(0, this._ctlBreadCrumbPanel.Children.Count);

                string currentPath = "";

                // Add bucket entry to represent the root.
                this._ctlBreadCrumbPanel.Children.Add(createBreadCrumbImage(currentPath, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.bucket.png"));
                this._ctlBreadCrumbPanel.Children.Add(createBreadCrumbTextBlock(currentPath, this._model.BucketName));


                string[] pathItems = this._model.Path.Split('/');
                foreach (string item in pathItems)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;

                    currentPath = string.Format("{0}{1}/", currentPath, item);
                    TextBlock separator = new TextBlock();
                    separator.Text = " > ";
                    this._ctlBreadCrumbPanel.Children.Add(separator);

                    this._ctlBreadCrumbPanel.Children.Add(createBreadCrumbImage(currentPath, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder.png"));
                    this._ctlBreadCrumbPanel.Children.Add(createBreadCrumbTextBlock(currentPath, item));
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error building breadcrumb", e);
            }
        }

        void breadCrumbMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UIElement block = sender as UIElement;
            if (block == null)
                return;
            updateBreadCrumb(block.Uid);
        }

        private void updateBreadCrumb(string path)
        {
            int pos = path.IndexOf('-');
            path = path.Substring(pos + 1);
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);
            if (path.StartsWith("/"))
                path = path.Substring(1);
            this._model.Path = path;
        }


        private TextBlock createBreadCrumbTextBlock(string breadCrumbpath, string title)
        {
            Style s = this._ctlBreadCrumbPanel.FindResource("BreadCrumbItem") as Style;
            TextBlock textItem = new TextBlock();
            textItem.Cursor = Cursors.Hand;
            textItem.Style = s;
            textItem.Text = title;
            textItem.Uid = "textblock-" + breadCrumbpath;
            textItem.VerticalAlignment = VerticalAlignment.Center;
            textItem.Margin = new Thickness(3, 0, 0, 0);
            textItem.MouseLeftButtonDown += new MouseButtonEventHandler(breadCrumbMouseLeftButtonDown);
            return textItem;
        }

        private Image createBreadCrumbImage(string breadCrumbpath, string path)
        {
            Image img = new Image();
            img.Cursor = Cursors.Hand;
            img.Uid = "image-" + breadCrumbpath;
            img.MouseLeftButtonDown += new MouseButtonEventHandler(breadCrumbMouseLeftButtonDown);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var bi = new BitmapImage();
            bi.DecodePixelHeight = 16;
            bi.DecodePixelWidth = 16;
            bi.BeginInit();
            bi.StreamSource = stream;
            bi.EndInit();
            img.Source = bi;
            img.Width = img.Height = 16;
            return img;
        }
        #endregion

        #region Toolbar Actions

        void refreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing S3 bucket model", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading folders and files: " + e.Message);
            }
        }

        void uploadFileClick(object sender, RoutedEventArgs args)
        {
            uploadFileClick();
        }

        void uploadFileClick()
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.CheckPathExists = true;
                dlg.Multiselect = true;

                if (!dlg.ShowDialog().GetValueOrDefault())
                {
                    return;
                }

                uploadFiles(dlg.FileNames, new FileInfo(dlg.FileName).DirectoryName);
            }
            catch (Exception e)
            {
                _logger.Error("Error uploading file", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error uploading file: " + e.Message);
            }
        }

        void uploadDirectoryClick(object sender, RoutedEventArgs args)
        {
            uploadDirectoryClick();
        }

        void uploadDirectoryClick()
        {
            try
            {
                string directory = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Select a directory to upload.");
                if (string.IsNullOrEmpty(directory))
                    return;

                string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                uploadFiles(files, Directory.GetParent(directory).FullName);
            }
            catch (Exception e)
            {
                _logger.Error("Error uploading directory", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error uploading directory: " + e.Message);
            }
        }

        void uploadFiles(string[] filenames, string localRoot)
        {
            try
            {
                string s3RootFolder = Model.Path;
                if (s3RootFolder.Length > 0 && !s3RootFolder.EndsWith("/"))
                    s3RootFolder += "/";

                NewUploadSettingsController settingsController = new NewUploadSettingsController();
                if (!settingsController.Execute())
                    return;

                var bucketAccessList = this._controller.GetBucketAccessList();
                var accessList = Permission.ConvertToAccessControlList(settingsController.Model.PermissionEntries, Permission.PermissionMode.Object, settingsController.Model.MakePublic);
                accessList.Owner = bucketAccessList.Owner;
                accessList.Grants.Add(new S3Grant() { Grantee = new S3Grantee() { CanonicalUser = bucketAccessList.Owner.Id }, Permission = S3Permission.FULL_CONTROL });

                NameValueCollection nvcMetadata;
                NameValueCollection nvcHeader;
                Metadata.GetMetadataAndHeaders(settingsController.Model.MetadataEntries, out nvcMetadata, out nvcHeader);

                var tags = new List<Tag>();
                if(settingsController.Model.Tags != null)
                {
                    foreach(var tag in settingsController.Model.Tags)
                    {
                        tags.Add(tag);
                    }
                }

                IJob job = null;
                if (filenames.Length > 0)
                {
                    job = new UploadMultipleFilesJob(this._controller, settingsController.Model, filenames, localRoot, s3RootFolder,
                            accessList, nvcMetadata, nvcHeader, tags);
                }

                if (job != null)
                {
                    this._ctlJobTracker.AddJob(job);
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error uploading file", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error uploading file: " + e.Message);
            }
        }


        void onCreateFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this._controller.CreateFolder();
                resortGrid();                
            }
            catch (Exception ex)
            {
                this._logger.Error("Error creating folder", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating folder: " + ex.Message);
            }
        }

        void onReducedRedundencyStorageClassClick(object sender, RoutedEventArgs e)
        {
            changeStorageClass(S3StorageClass.ReducedRedundancy);
        }

        void onStandardStorageClassClick(object sender, RoutedEventArgs e)
        {
            changeStorageClass(S3StorageClass.Standard);
        }

        void changeStorageClass(S3StorageClass storageClass)
        {
            List<BucketBrowserModel.ChildItem> childItems = getSelectedItemsAsList();

            IJob job = new ChangeStorageClassJob(this._controller, childItems, storageClass);
            this._ctlJobTracker.AddJob(job);
        }

        void onNoEncryptionClick(object sender, RoutedEventArgs e)
        {
            changeServerSideEncryption(ServerSideEncryptionMethod.None);
        }

        void onAES256Click(object sender, RoutedEventArgs e)
        {
            changeServerSideEncryption(ServerSideEncryptionMethod.AES256);
        }

        void changeServerSideEncryption(ServerSideEncryptionMethod method)
        {
            List<BucketBrowserModel.ChildItem> childItems = getSelectedItemsAsList();

            IJob job = new ChangeServerSideEncryptionJob(this._controller, childItems, method);
            this._ctlJobTracker.AddJob(job);
        }

        void onRequestInvalidationClick(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                this._controller.PerformInvalidationRequest(selectedItems);
                ToolkitFactory.Instance.ShellProvider.ShowMessage("CloudFront", "Invalidation request sent to Amazon CloudFront.");
            }
            catch (Exception ex)
            {
                this._logger.Error("Error performing CloudFront invalidation request", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error performing CloudFront invalidation request: " + ex.Message);
            }
        }

        #endregion

        #region Filter

        Guid _lastTextFilterChangeToken;

        void onTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return; this._lastTextFilterChangeToken = Guid.NewGuid();

            ThreadPool.QueueUserWorkItem(this.asyncRefresh,
                new LoadState(this._lastTextFilterChangeToken, false, false));
        }

        void asyncRefresh(object state)
        {
            if (!(state is LoadState))
                return;
            LoadState loadState = (LoadState)state;

            try
            {
                if (loadState.LastTextFilterChangeToken != Guid.Empty)
                    Thread.Sleep(Constants.TEXT_FILTER_IDLE_TIMER);

                if (loadState.LastTextFilterChangeToken == Guid.Empty || this._lastTextFilterChangeToken == loadState.LastTextFilterChangeToken)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        this._model.ChildItems.ReApplyFilter();
                        this.resortGrid();
                    }));
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error applying filter", e);
            }
        }

        #endregion

        #region Build Context Menu
        MenuItem _makePublicItem;
        MenuItem _openItem;
        MenuItem _downloadItem;
        MenuItem _propertiesItem;
        MenuItem _deleteItem;
        MenuItem _copyItem;
        MenuItem _cutItem;
        MenuItem _pasteItem;
        MenuItem _pasteIntoItem;
        MenuItem _renameItem;
        MenuItem _createFolder;
        MenuItem _upload;
        MenuItem _changeStorageClass;
        MenuItem _changeServerSideEncryption;
        MenuItem _cloudFrontInvalidation;
        MenuItem _generatePreSignedURL;
        MenuItem _restoreItem;
        MenuItem _invokeLamda;


        private void buildMenuItems()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            this._makePublicItem = new MenuItem();
            this._makePublicItem.Header = "Make Publicly Readable";
            this._makePublicItem.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.make-public.png");
            this._makePublicItem.Click += new RoutedEventHandler(onMakePublicRows);

            this._openItem = new MenuItem();
            this._openItem.Header = "Open";
            this._openItem.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.open.png");
            this._openItem.Click += new RoutedEventHandler(onOpen);

            this._downloadItem = new MenuItem();
            this._downloadItem.Header = "Download";
            this._downloadItem.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.download.png");
            this._downloadItem.Click += new RoutedEventHandler(onDownloadRows);

            this._propertiesItem = new MenuItem();
            this._propertiesItem.Header = "Properties...";
            this._propertiesItem.Icon = IconHelper.GetIcon("properties.png");
            this._propertiesItem.Click += new RoutedEventHandler(onPropertiesRows);

            this._deleteItem = new MenuItem();
            this._deleteItem.Header = "Delete";
            this._deleteItem.Icon = IconHelper.GetIcon("delete.png");
            this._deleteItem.Click += new RoutedEventHandler(onDeleteRows);

            this._copyItem = new MenuItem();
            this._copyItem.Header = "Copy";
            this._copyItem.Icon = IconHelper.GetIcon("copy.png");
            this._copyItem.Click += new RoutedEventHandler(onCopyRows);

            this._cutItem = new MenuItem();
            this._cutItem.Header = "Cut";
            this._cutItem.Icon = IconHelper.GetIcon("cut.png");
            this._cutItem.Click += new RoutedEventHandler(onCutRows);

            this._pasteItem = new MenuItem();
            this._pasteItem.Header = "Paste";
            this._pasteItem.Icon = IconHelper.GetIcon("paste.png");
            this._pasteItem.Click += new RoutedEventHandler(onPasteRows);

            this._pasteIntoItem = new MenuItem();
            this._pasteIntoItem.Header = "Paste Into";
            this._pasteIntoItem.Icon = IconHelper.GetIcon("paste.png");
            this._pasteIntoItem.Click += new RoutedEventHandler(onPasteRows);

            this._renameItem = new MenuItem();
            this._renameItem.Header = "Rename";
            this._renameItem.Icon = IconHelper.GetIcon("paste.png");
            this._renameItem.Click += new RoutedEventHandler(onRenameFile);

            this._createFolder = new MenuItem();
            this._createFolder.Header = "Create Folder...";
            this._createFolder.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.create-folder.png");
            this._createFolder.Click += new RoutedEventHandler(onCreateFolderClick);

            this._upload = new MenuItem();
            this._upload.Header = "Upload";
            this._upload.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.upload.png");

            this._restoreItem = new MenuItem();
            this._restoreItem.Header = "Initiate Restore From Glacier...";
            this._restoreItem.ToolTip = "Restore object(s) that have been transitioned to Amazon Glacier";
            this._restoreItem.Click += onRestore;

            this._invokeLamda = new MenuItem();
            this._invokeLamda.Header = "Invoke Lambda Function...";
            this._invokeLamda.ToolTip = "Invokes a Lambda function with object keys as arguments to the function";
            this._invokeLamda.Click += onInvokeLambda;

            MenuItem uploadFile = new MenuItem();
            uploadFile.Header = "File";
            uploadFile.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.file_up.png");
            uploadFile.Click += new RoutedEventHandler(uploadFileClick);
            this._upload.Items.Add(uploadFile);

            MenuItem uploadDirectory = new MenuItem();
            uploadDirectory.Header = "Folder";
            uploadDirectory.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder_up.png");
            uploadDirectory.Click += new RoutedEventHandler(uploadDirectoryClick);
            this._upload.Items.Add(uploadDirectory);

            this._changeStorageClass = new MenuItem();
            this._changeStorageClass.Header = "Change Storage Class";

            MenuItem reducedRedundencyStorageClass = new MenuItem();
            reducedRedundencyStorageClass.Header = "Reduced Redundancy";
            reducedRedundencyStorageClass.Click += new RoutedEventHandler(onReducedRedundencyStorageClassClick);
            this._changeStorageClass.Items.Add(reducedRedundencyStorageClass);

            MenuItem standardStorageClass = new MenuItem();
            standardStorageClass.Header = "Standard";
            standardStorageClass.Click += new RoutedEventHandler(onStandardStorageClassClick);
            this._changeStorageClass.Items.Add(standardStorageClass);

            this._changeServerSideEncryption = new MenuItem();
            this._changeServerSideEncryption.Header = "Change Encryption";

            MenuItem noEncryption = new MenuItem();
            noEncryption.Header = "None";
            noEncryption.Click += new RoutedEventHandler(onNoEncryptionClick);
            this._changeServerSideEncryption.Items.Add(noEncryption);

            MenuItem aes256Encryption = new MenuItem();
            aes256Encryption.Header = "AES-256";
            aes256Encryption.Click += new RoutedEventHandler(onAES256Click);
            this._changeServerSideEncryption.Items.Add(aes256Encryption);


            this._cloudFrontInvalidation = new MenuItem();
            this._cloudFrontInvalidation.Header = "CloudFront Invalidate";
            this._cloudFrontInvalidation.Icon = IconHelper.GetIcon(assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.cloudfront-distribution.png");
            this._cloudFrontInvalidation.Click += new RoutedEventHandler(onRequestInvalidationClick);

            this._generatePreSignedURL = new MenuItem();
            this._generatePreSignedURL.Header = "Create Pre-Signed URL...";
            this._generatePreSignedURL.Click += new RoutedEventHandler(onGeneratePreSignedURL);

        }

        private void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            this.buildMenuItems();
            ContextMenu menu = new ContextMenu();

            List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();

            bool glacierItemsOnlySelected = !selectedItems.Any(x =>
                {
                    if (x.StorageClass == null || x.StorageClass.Value != S3StorageClass.Glacier)
                        return true;

                    return false;
                });

            this._makePublicItem.IsEnabled = selectedItems.Count > 0;
            this._openItem.IsEnabled = selectedItems.Count > 0;
            this._downloadItem.IsEnabled = selectedItems.Count > 0;
            this._propertiesItem.IsEnabled = selectedItems.Count == 1;
            this._generatePreSignedURL.IsEnabled = selectedItems.Count == 1;
            this._deleteItem.IsEnabled = selectedItems.Count > 0;
            this._renameItem.IsEnabled = selectedItems.Count == 1 && !glacierItemsOnlySelected;

            this._copyItem.IsEnabled = selectedItems.Count > 0 && !glacierItemsOnlySelected;
            this._cutItem.IsEnabled = selectedItems.Count > 0 && !glacierItemsOnlySelected;
            this._pasteItem.IsEnabled = this._controller.ClipboardContainer.Clipboard != null || System.Windows.Clipboard.ContainsFileDropList();
            this._pasteIntoItem.IsEnabled = this._controller.ClipboardContainer.Clipboard != null || System.Windows.Clipboard.ContainsFileDropList();
            this._restoreItem.IsEnabled = selectedItems.Any(x => x.StorageClass == S3StorageClass.Glacier || x.ChildType == BucketBrowserModel.ChildType.Folder);

            this._invokeLamda.IsEnabled = selectedItems.Count > 0 && !glacierItemsOnlySelected;

            this._changeStorageClass.IsEnabled = !glacierItemsOnlySelected;
            this._changeServerSideEncryption.IsEnabled = !glacierItemsOnlySelected;

            menu.Items.Add(this._createFolder);
            menu.Items.Add(this._upload);
            if (selectedItems.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
            {
                menu.Items.Add(this._openItem);
            }

            if (selectedItems.Count > 0)
            {
                menu.Items.Add(this._downloadItem);
                menu.Items.Add(this._makePublicItem);
                menu.Items.Add(this._deleteItem);
                menu.Items.Add(this._changeStorageClass);
                menu.Items.Add(this._changeServerSideEncryption);
                menu.Items.Add(this._invokeLamda);
                menu.Items.Add(this._restoreItem);
            }

            if (selectedItems.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
            {
                menu.Items.Add(this._renameItem);
            }

            if (selectedItems.Count > 0)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(this._cutItem);
                menu.Items.Add(this._copyItem);
            }

            if (selectedItems.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.Folder)
            {
                this._pasteIntoItem.Uid = selectedItems[0].FullPath;
                menu.Items.Add(this._pasteIntoItem);
            }
            else
            {
                this._pasteItem.Uid = this._model.Path;
                menu.Items.Add(this._pasteItem);
            }

            if (selectedItems.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(this._propertiesItem);
                menu.Items.Add(this._generatePreSignedURL);

                addCopyToClipboardMenuItems(menu, selectedItems[0]);
            }

            if (selectedItems.Count > 0 && this._controller.CloudFrontDistributions.Count > 0)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(this._cloudFrontInvalidation);
            }


            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        private void addCopyToClipboardMenuItems(ContextMenu menu, BucketBrowserModel.ChildItem item)
        {
            MenuItem copyUrl = new MenuItem();
            copyUrl.Header = "Copy URL to Clipboard";
            copyUrl.Click += delegate(object s, RoutedEventArgs evnt)
            {
                try
                {
                    var url = this._controller.S3Client.GetPublicURL(this._controller.Model.BucketName, item.FullPath);
                    System.Windows.Clipboard.SetText(url.ToString());
                }
                catch (Exception ex)
                {
                    _logger.Error("Error copying URL to clipboard", ex);
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error copying URL to clipboard: " + ex.Message);
                }
            };
            menu.Items.Add(copyUrl);

            if (this._controller.CloudFrontDistributions.Count > 0)
            {
                MenuItem copyDistributionUrl = new MenuItem();
                copyDistributionUrl.Header = "Copy Distribution URLs";

                foreach (var dis in this._controller.CloudFrontDistributions)
                {
                    if (dis.Aliases.Items.Count> 0)
                    {
                        foreach (var cname in dis.Aliases.Items)
                        {
                            copyDistributionUrl.Items.Add(createCopyToDistributionMenuItem(cname, item.FullPath));
                        }
                    }
                    else
                    {
                        copyDistributionUrl.Items.Add(createCopyToDistributionMenuItem(dis.DomainName, item.FullPath));
                    }
                }

                menu.Items.Add(copyDistributionUrl);

            }
        }

        private MenuItem createCopyToDistributionMenuItem(string domain, string path)
        {
            var mi = new MenuItem();
            mi.Header = string.Format("http://{0}/{1}", domain, path);
            mi.Click += delegate(object s, RoutedEventArgs evnt)
            {
                try
                {
                    System.Windows.Clipboard.SetText(mi.Header.ToString());
                }
                catch (Exception ex)
                {
                    _logger.Error("Error copying URL to clipboard", ex);
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error copying URL to clipboard: " + ex.Message);
                }
            };

            return mi;
        }

        #endregion

        #region ContextMenu Callbacks

        void onRestore(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count == 0)
                    return;

                var controller = new RestoreObjectPromptController(this._controller.S3Client);
                if (!controller.Execute())
                    return;

                int days;
                if (!int.TryParse(controller.Model.RestoreDays, out days))
                    return;

                var job = new RestoreObjectsJob(this._controller, selectedItems, days);
                this._ctlJobTracker.AddJob(job);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error restoring object(s)", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error restoring object(s): " + ex.Message);
            }
        }

        void onInvokeLambda(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count == 0)
                    return;

                var listOfKeys = this._controller.GetListOfKeys(selectedItems, false);

                var controller = new InvokeLambdaFunctionController(this._controller.S3Client, this.Model.BucketName, listOfKeys);
                controller.Execute();
            }
            catch (Exception ex)
            {
                this._logger.Error("Error invoking Lambda function", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error invoking Lambda function(s): " + ex.Message);
            }
        }

        void onGeneratePreSignedURL(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count != 1 || selectedItems[0].ChildType != BucketBrowserModel.ChildType.File)
                    return;

                this._controller.GeneratePreSignedURL(selectedItems[0].FullPath);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error generating pre-signed URL", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error generating pre-signed URL: " + ex.Message);
            }
        }

        void onOpen(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count != 1 || selectedItems[0].ChildType != BucketBrowserModel.ChildType.File)
                    return;

                this._controller.Open(selectedItems[0].FullPath);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error opening file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening file: " + ex.Message);
            }
        }

        void onPropertiesRows(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count != 1 || selectedItems[0].ChildType != BucketBrowserModel.ChildType.File)
                    return;

                this._controller.ShowObjectProperties(selectedItems[0]);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error displaying properties", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error displaying properties: " + ex.Message);
            }
        }

        void onRenameFile(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                if (selectedItems == null || selectedItems.Count != 1 || selectedItems[0].ChildType != BucketBrowserModel.ChildType.File)
                    return;

                this._controller.Rename(selectedItems[0]);
                resortGrid();
            }
            catch (Exception ex)
            {
                this._logger.Error("Error renaming file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error renaming file: " + ex.Message);
            }
        }

        void onDeleteRows(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> childItems = new List<BucketBrowserModel.ChildItem>();
                foreach (BucketBrowserModel.ChildItem childItem in this._ctlDataGrid.SelectedItems)
                {
                    if (childItem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
                        continue;

                    // Not sure how we are getting folders with empty names but 
                    // when we delete them it deletes all the data.  For now just blocking
                    // on these deletes.
                    if (childItem.Title.Equals(""))
                    {
                        ToolkitFactory.Instance.ShellProvider.ShowError("Can not delete items with no names");
                        return;
                    }

                    childItems.Add(childItem);
                }

                string confirmMsg;
                if (childItems.Count == 1)
                    confirmMsg = string.Format("Are you sure you want to permanently delete \"{0}\"?", childItems[0].Title);
                else
                    confirmMsg = string.Format("Are you sure you want to permanently delete {0} items?", childItems.Count);

                if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Items?", confirmMsg))
                {
                    DeleteFilesJob job = new DeleteFilesJob(this._controller, childItems);
                    this._ctlJobTracker.AddJob(job);
                }
            }
            catch (Exception ex)
            {
                this._logger.Error("Error deleting", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting: " + ex.Message);
            }
        }

        void onDownloadRows(object sender, RoutedEventArgs e)
        {
            try
            {
                string downloadLocation = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Choose a folder to download files.");
                if (!string.IsNullOrEmpty(downloadLocation))
                {
                    List<BucketBrowserModel.ChildItem> childItems = getSelectedItemsAsList();
                    DownloadFilesJob job = new DownloadFilesJob(this._controller, this._controller.BucketName, this._model.Path, childItems.ToArray(), downloadLocation);
                    this._ctlJobTracker.AddJob(job);
                }
            }
            catch (Exception ex)
            {
                this._logger.Error("Error downloading", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error downloading: " + ex.Message);
            }
        }

        void onMakePublicRows(object sender, RoutedEventArgs e)
        {
            try
            {
                List<BucketBrowserModel.ChildItem> childItems = getSelectedItemsAsList();
                IJob job = new MakePublicJob(this._controller, childItems);
                this._ctlJobTracker.AddJob(job);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error making public", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error making public: " + ex.Message);
            }
        }

        void onCutRows(object sender, RoutedEventArgs e)
        {
            this._controller.ClipboardContainer.Clipboard = new S3Clipboard(S3Clipboard.ClipboardMode.Cut, this._controller, this._controller.Model.Path, getSelectedItemsAsList());
            System.Windows.Clipboard.Clear();
        }

        void onCopyRows(object sender, RoutedEventArgs e)
        {
            this._controller.ClipboardContainer.Clipboard = new S3Clipboard(S3Clipboard.ClipboardMode.Copy, this._controller, this._controller.Model.Path, getSelectedItemsAsList());
            System.Windows.Clipboard.Clear();
        }

        void onPasteRows(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Clipboard.GetDataObject().GetDataPresent(DataFormats.FileDrop))
            {
                onFileDrop(System.Windows.Clipboard.GetDataObject());
                return;
            }

            if (this._controller.ClipboardContainer.Clipboard == null)
                return;

            try
            {
                string path;
                if (sender is MenuItem)
                {
                    MenuItem item = sender as MenuItem;
                    path = item.Uid;
                }
                else
                {
                    path = this._controller.Model.Path;
                }

                PasteFilesJob job = new PasteFilesJob(this._controller,
                    this._controller.ClipboardContainer.Clipboard, path);
                if (this._controller.ClipboardContainer.Clipboard.Mode == S3Clipboard.ClipboardMode.Cut)
                {
                    this._controller.ClipboardContainer.Clipboard = null;
                }

                this._ctlJobTracker.AddJob(job);
            }
            catch (Exception ex)
            {
                this._logger.Error("Error pasting", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error pasting: " + ex.Message);
            }
        }

        List<BucketBrowserModel.ChildItem> getSelectedItemsAsList()
        {
            var itemsInClipboard = new List<BucketBrowserModel.ChildItem>();
            foreach (BucketBrowserModel.ChildItem selectedItem in this._ctlDataGrid.SelectedItems)
            {
                if (selectedItem.ChildType != BucketBrowserModel.ChildType.LinkToParent)
                {
                    itemsInClipboard.Add(selectedItem);
                }
            }

            return itemsInClipboard;
        }
        #endregion

        #region Drag and Drop Support

        private void onFileDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent("FileDrop"))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void onFileDrop(object sender, DragEventArgs e)
        {
            onFileDrop(e.Data);
        }

        private void onFileDrop(IDataObject data)
        {
            try
            {
                string[] dataItems = data.GetData(DataFormats.FileDrop) as string[];
                if (dataItems == null)
                    return;

                List<string> files = new List<string>();
                foreach (var dataItem in dataItems)
                {
                    if (File.Exists(dataItem))
                    {
                        files.Add(dataItem);
                    }
                    else if (Directory.Exists(dataItem))
                    {
                        foreach (var filename in Directory.GetFiles(dataItem, "*", SearchOption.AllDirectories))
                        {
                            files.Add(filename);
                        }
                    }
                }

                if (dataItems.Length == 0)
                    return;

                string parent;
                if (File.Exists(dataItems[0]))
                {
                    parent = new FileInfo(dataItems[0]).DirectoryName;
                }
                else
                {
                    parent = new DirectoryInfo(dataItems[0]).Parent.FullName;
                }

                uploadFiles(files.ToArray(), parent);
            }
            catch (Exception ex)
            {
                _logger.Error("Error on file drop", ex);
            }
        }

        Point _lastMouseDown;
        void onDataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(this._ctlDataGrid);
            }
        }

        void onDataGridMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && this._controller.S3RootViewModel != null)
                {
                    if (UIUtils.IsPointOnScrollBar(_lastMouseDown, this._ctlDataGrid))
                        return;

                    Point currentPosition = e.GetPosition(this._ctlDataGrid);
                    Console.WriteLine(currentPosition.ToString());

                    if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                    {
                        List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();
                        if (selectedItems.Count > 0)
                        {
                            this._ctlDataGrid.AllowDrop = false;
                            var s3DataObject = new S3DataObject(this._controller.S3RootViewModel, this._model.BucketName, this._model.Path, selectedItems);
                            s3DataObject.SetData(DataFormats.StringFormat, s3DataObject);
                            DragDrop.DoDragDrop(this._ctlDataGrid, s3DataObject, DragDropEffects.Copy);
                            this._ctlDataGrid.AllowDrop = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Special Sort for Size Column

        void onNewChildItems(object sender, EventArgs e)
        {
            resortGrid();
        }

        void resortGrid()
        {
            try
            {
                ListCollectionView lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(this._ctlDataGrid.ItemsSource);
                IComparer<BucketBrowserModel.ChildItem> comparer = lcv.CustomSort as IComparer<BucketBrowserModel.ChildItem>;
                if (comparer == null)
                {
                    comparer = new NameComparer(ListSortDirection.Descending);
                }
                this._controller.Model.ChildItems.SortDisplayedChildItems(comparer);
            }
            catch
            {
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            DataGridColumn column = e.Column;
            ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;
            column.SortDirection = direction;

            IComparer comparer;

            if (column.Header.ToString().Equals("Size"))
                comparer = new SizeComparer(direction);
            else if (column.Header.ToString().Equals("Last Modified Date"))
                comparer = new DateComparer(direction);
            else
                comparer = new NameComparer(direction);

            //use a ListCollectionView to do the sort.
            ListCollectionView lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(this._ctlDataGrid.ItemsSource);
            lcv.CustomSort = comparer;
        }

        #endregion

        #region Grid Shortcut Keys

        private void CopyCommandExecuted(object sender, ExecutedRoutedEventArgs e) 
        {
            this.onCopyRows(sender, e);
        }

        private void CutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.onCutRows(sender, e);
        }

        private void PasteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.onPasteRows(sender, e);
        }

        private void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            navigateUpDirectory();
        }

        private void navigateUpDirectory()
        {
            if (this._ctlBreadCrumbPanel.Children.Count < 4)
                return;

            /// Subtract 3 from the end for the current label, angle bracket and image.
            var previous = this._ctlBreadCrumbPanel.Children[(this._ctlBreadCrumbPanel.Children.Count - 1) - 3];
            updateBreadCrumb(previous.Uid);
        }

        private void onGridKeyDown(object sender, KeyEventArgs e)
        {
            List<BucketBrowserModel.ChildItem> selectedItems = getSelectedItemsAsList();

            if (e.Key == Key.Delete && selectedItems.Count > 0)
            {
                this.onDeleteRows(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (this._ctlDataGrid.SelectedItems.Count == 1)
                {
                    RowMouseDoubleClick(sender, null);
                    e.Handled = true;
                }
            }
        }

        #endregion

        private void onCancelLoad(object sender, MouseButtonEventArgs e)
        {
            this._controller.CancelFetchingObjects();
        }


        bool _isOnCancelHandlerAdded = false;
        public void UpdateFetchingStatus(string message, bool enableCancel)
        {
            this._ctlFetchStatus.Text = message;

            if (enableCancel)
            {
                if(!this._isOnCancelHandlerAdded)
                {
                    this._ctlFetchCancel.MouseLeftButtonDown += onCancelLoad;
                    this._isOnCancelHandlerAdded = true;
                }
                this._ctlFetchCancel.Cursor = Cursors.Hand;
                this._ctlFetchCancel.Foreground = FindResource("awsUrlTextFieldForegroundBrushKey") as SolidColorBrush;
            }
            else
            {
                if(this._isOnCancelHandlerAdded)
                {
                    this._ctlFetchCancel.MouseLeftButtonDown -= onCancelLoad;
                    this._isOnCancelHandlerAdded = false;
                }
                this._ctlFetchCancel.Cursor = Cursors.Arrow;
                this._ctlFetchCancel.Foreground = FindResource("awsDisabledControlForegroundBrushKey") as SolidColorBrush; 
            }
        }
    }
}
