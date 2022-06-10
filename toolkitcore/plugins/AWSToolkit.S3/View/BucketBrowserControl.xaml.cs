using System;
using System.Collections;
using System.Collections.Generic;
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
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.CommonUI.JobTracker;
using Amazon.AWSToolkit.S3.Clipboard;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Jobs;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for BucketBrowserControl.xaml
    /// </summary>
    public partial class BucketBrowserControl : BaseAWSControl
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(BucketBrowserControl));

        private readonly BucketBrowserController _controller;
        private readonly BucketBrowserModel _model;

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
            _controller = controller;
            _model = model;

            _model.PropertyChanged += OnModelPropertyChanged;
            _model.NewItems += OnNewChildItems;
            InitializeComponent();

            BuildBreadCrumb();
            _ctlDataGrid.SelectedItem = null;

            Unloaded += (sender, e) => _controller.DisposeLoadingThread();
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            _controller.StartFetchingObject();            
            return _controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            _controller.RecordOpenEditorMetric(success ? Result.Succeeded : Result.Failed);
        }

        public BucketBrowserModel Model => DataContext as BucketBrowserModel;

        public override string Title => $"S3 Bucket: {_controller.BucketName}";

        public override string UniqueId => $"S3:BucketBrowser:{_controller.BucketName}";

        private void RowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // github.com/aws/aws-sdk-net/issues/160, double click on scroll bar
            // was opening the object. Note that the row header image does not report as
            // having a parent
            bool isDataGridCellOrImage =
                e.MouseDevice.DirectlyOver is FrameworkElement frameworkElement &&
                (
                    frameworkElement.Parent is DataGridCell ||
                    (
                        frameworkElement.Parent == null &&
                        frameworkElement is Image
                    )
                );

            if (isDataGridCellOrImage && _ctlDataGrid.SelectedItem is BucketBrowserModel.ChildItem childItem)
            {
                switch (childItem.ChildType)
                {
                    case BucketBrowserModel.ChildType.LinkToParent:
                        NavigateUpDirectory();
                        break;
                    case BucketBrowserModel.ChildType.Folder:
                        Model.Path = childItem.FullPath;
                        break;
                    case BucketBrowserModel.ChildType.File:
                        _controller.Open(childItem.FullPath);
                        break;
                }
            }
        }

        public void SetDataGridFocus()
        {
            if (_ctlDataGrid.Items.Count > 0)
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread(() =>
                {
                    _ctlDataGrid.Focus();
                    DataGridHelper.GetCell(_ctlDataGrid, 0, 0)?.Focus();
                });
            }
        }

        #region Bread Crumb Operation(s)

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Path)))
            {
                BuildBreadCrumb();
            }
        }

        private void BuildBreadCrumb()
        {
            try
            {
                // Clear out existing breadcrumb
                _ctlBreadCrumbPanel.Children.RemoveRange(0, _ctlBreadCrumbPanel.Children.Count);

                string currentPath = "";

                // Add bucket entry to represent the root.
                _ctlBreadCrumbPanel.Children.Add(CreateS3BucketBreadCrumbImage());
                _ctlBreadCrumbPanel.Children.Add(CreateBreadCrumbTextBlock(currentPath, _model.BucketName));

                string[] pathItems = _model.Path.Split('/');
                foreach (string item in pathItems)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }

                    currentPath = $"{currentPath}{item}/";
                    TextBlock separator = new TextBlock
                    {
                        Text = " > "
                    };

                    _ctlBreadCrumbPanel.Children.Add(separator);
                    _ctlBreadCrumbPanel.Children.Add(CreateBreadCrumbImage(currentPath, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder.png"));
                    _ctlBreadCrumbPanel.Children.Add(CreateBreadCrumbTextBlock(currentPath, item));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error building breadcrumb", ex);
            }
        }

        private void BreadCrumbMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement uiElement)
            {
                UpdateBreadCrumb(uiElement.Uid);
            }
        }

        private void UpdateBreadCrumb(string breadCrumbPath)
        {
            breadCrumbPath = breadCrumbPath.Substring(breadCrumbPath.IndexOf('-') + 1);

            if (breadCrumbPath.EndsWith("/"))
            {
                breadCrumbPath = breadCrumbPath.Substring(0, breadCrumbPath.Length - 1);
            }

            if (breadCrumbPath.StartsWith("/"))
            {
                breadCrumbPath = breadCrumbPath.Substring(1);
            }

            _model.Path = breadCrumbPath;
        }

        private TextBlock CreateBreadCrumbTextBlock(string breadCrumbPath, string title)
        {
            var textBlock = new TextBlock
            {
                Cursor = Cursors.Hand,
                Style = _ctlBreadCrumbPanel.FindResource("BreadCrumbItem") as Style,
                Text = title,
                Uid = $"textblock-{breadCrumbPath}",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3, 0, 0, 0)
            };
            textBlock.MouseLeftButtonDown += BreadCrumbMouseLeftButtonDown;

            return textBlock;
        }

        private Image CreateS3BucketBreadCrumbImage()
        {
            var img = new Image
            {
                Cursor = Cursors.Hand,
                Source = ToolkitImages.SimpleStorageService,
                Height = 16,
                Width = 16
            };
            img.MouseLeftButtonDown += BreadCrumbMouseLeftButtonDown;

            return img;
        }

        private Image CreateBreadCrumbImage(string breadCrumbPath, string imagePath)
        {
            var bi = new BitmapImage
            {
                DecodePixelHeight = 16,
                DecodePixelWidth = 16
            };
            bi.BeginInit();
            bi.StreamSource = Assembly.GetExecutingAssembly().GetManifestResourceStream(imagePath);
            bi.EndInit();

            var img = new Image
            {
                Cursor = Cursors.Hand,
                Uid = $"image-{breadCrumbPath}",
                Source = bi,
                Height = 16,
                Width = 16
            };
            img.MouseLeftButtonDown += BreadCrumbMouseLeftButtonDown;

            return img;
        }
        #endregion

        #region Toolbar Action(s)

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _controller.Refresh();
            }
            catch (Exception ex)
            {
                _logger.Error("Error refreshing S3 bucket model", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error loading folders and files: {ex.Message}");
            }
        }

        private void UploadFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    CheckPathExists = true,
                    Multiselect = true
                };

                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    UploadFiles(dlg.FileNames, new FileInfo(dlg.FileName).DirectoryName);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error uploading file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error uploading file: {ex.Message}");
            }
        }

        private void UploadDirectoryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var directory = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Select a directory to upload.");
                if (string.IsNullOrEmpty(directory))
                {
                    return;
                }

                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                UploadFiles(files, Directory.GetParent(directory).FullName);
            }
            catch (Exception ex)
            {
                _logger.Error("Error uploading directory", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error uploading directory: {ex.Message}");
            }
        }

        private void UploadFiles(string[] filenames, string localRoot)
        {
            try
            {
                var s3RootFolder = Model.Path;
                if (s3RootFolder.Length > 0 && !s3RootFolder.EndsWith("/"))
                {
                    s3RootFolder += "/";
                }

                var settingsController = new NewUploadSettingsController();
                if (!settingsController.Execute())
                {
                    return;
                }

                var bucketAccessList = _controller.GetBucketAccessList();

                var accessList = Permission.ConvertToAccessControlList(settingsController.Model.PermissionEntries, Permission.PermissionMode.Object, settingsController.Model.MakePublic);
                accessList.Owner = bucketAccessList.Owner;
                accessList.Grants.Add(new S3Grant { Grantee = new S3Grantee { CanonicalUser = bucketAccessList.Owner.Id }, Permission = S3Permission.FULL_CONTROL });

                Metadata.GetMetadataAndHeaders(settingsController.Model.MetadataEntries, out var nvcMetadata, out var nvcHeader);

                var tags = new List<Tag>();
                if (settingsController.Model.Tags != null)
                {
                    foreach (var tag in settingsController.Model.Tags)
                    {
                        tags.Add(tag);
                    }
                }

                if (filenames.Length > 0)
                {
                    _ctlJobTracker.AddJob(new UploadMultipleFilesJob(_controller, settingsController.Model, filenames, localRoot, s3RootFolder,
                            accessList, nvcMetadata, nvcHeader, tags));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error uploading file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error uploading file: {ex.Message}");
            }
        }

        private void OnCreateFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _controller.CreateFolder();
                SortGrid();                
            }
            catch (Exception ex)
            {
                _logger.Error("Error creating folder", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error creating folder: {ex.Message}");
            }
        }

        private void ChangeStorageClass(S3StorageClass storageClass)
        {
            _ctlJobTracker.AddJob(new ChangeStorageClassJob(_controller, GetSelectedItemsAsList(), storageClass));
        }

        private void OnNoEncryptionClick(object sender, RoutedEventArgs e)
        {
            ChangeServerSideEncryption(ServerSideEncryptionMethod.None);
        }

        private void OnAes256Click(object sender, RoutedEventArgs e)
        {
            ChangeServerSideEncryption(ServerSideEncryptionMethod.AES256);
        }

        private void ChangeServerSideEncryption(ServerSideEncryptionMethod method)
        {
            _ctlJobTracker.AddJob(new ChangeServerSideEncryptionJob(_controller, GetSelectedItemsAsList(), method));
        }

        private void OnRequestInvalidationClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _controller.PerformInvalidationRequest(GetSelectedItemsAsList());
                ToolkitFactory.Instance.ShellProvider.ShowMessage("CloudFront", "Invalidation request sent to Amazon CloudFront.");
            }
            catch (Exception ex)
            {
                _logger.Error("Error performing CloudFront invalidation request", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error performing CloudFront invalidation request: {ex.Message}");
            }
        }

        #endregion

        #region Filter

        private Guid _lastTextFilterChangeToken;

        private void OnTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext is set
            if (IsEnabled)
            {
                _lastTextFilterChangeToken = Guid.NewGuid();
                ThreadPool.QueueUserWorkItem(AsyncRefresh, new LoadState(_lastTextFilterChangeToken, false, false));
            }
        }

        private void AsyncRefresh(object state)
        {
            if (state is LoadState loadState)
            {
                try
                {
                    if (loadState.LastTextFilterChangeToken != Guid.Empty)
                    {
                        Thread.Sleep(Constants.TEXT_FILTER_IDLE_TIMER);
                    }

                    if (loadState.LastTextFilterChangeToken == Guid.Empty || _lastTextFilterChangeToken == loadState.LastTextFilterChangeToken)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _model.ChildItems.ReApplyFilter();
                            SortGrid();
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error applying filter", ex);
                }
            }
        }

        #endregion

        #region Build Context Menu

        private MenuItem _makePublicItem;
        private MenuItem _openItem;
        private MenuItem _downloadItem;
        private MenuItem _propertiesItem;
        private MenuItem _deleteItem;
        private MenuItem _copyItem;
        private MenuItem _cutItem;
        private MenuItem _pasteItem;
        private MenuItem _pasteIntoItem;
        private MenuItem _renameItem;
        private MenuItem _createFolder;
        private MenuItem _upload;
        private MenuItem _changeStorageClass;
        private MenuItem _changeServerSideEncryption;
        private MenuItem _cloudFrontInvalidation;
        private MenuItem _generatePreSignedUrl;
        private MenuItem _restoreItem;
        private MenuItem _invokeLambda;

        private MenuItem BuildMenuItem(object header, RoutedEventHandler click = null, string iconEmbeddedName = null, Assembly iconAssembly = null, object tooltip = null)
        {
            var menuItem = new MenuItem
            {
                Header = header,
                ToolTip = tooltip
            };

            if (iconEmbeddedName != null)
            {
                menuItem.Icon = iconAssembly != null
                    ? IconHelper.GetIcon(iconAssembly, iconEmbeddedName)
                    : IconHelper.GetIcon(iconEmbeddedName);
            }

            if (click != null)
            {
                menuItem.Click += click;
            }

            return menuItem;
        }

        private void BuildMenuItems()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            _makePublicItem = BuildMenuItem("Make Publicly Readable", click: OnMakePublicRows,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.make-public.png", iconAssembly: assembly);

            _openItem = BuildMenuItem("Open", click: OnOpen,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.open.png", iconAssembly: assembly);

            _downloadItem = BuildMenuItem("Download...", click: OnDownloadRows,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.download.png", iconAssembly: assembly);

            _propertiesItem = BuildMenuItem("Properties...", click: OnPropertiesRows, iconEmbeddedName: "properties.png");

            _deleteItem = BuildMenuItem("Delete", click: OnDeleteRows, iconEmbeddedName: "delete.png");

            _copyItem = BuildMenuItem("Copy", click: OnCopyRows, iconEmbeddedName: "copy.png");

            _cutItem = BuildMenuItem("Cut", click: OnCutRows, iconEmbeddedName: "cut.png");

            _pasteItem = BuildMenuItem("Paste", click: OnPasteRows, iconEmbeddedName: "paste.png");

            _pasteIntoItem = BuildMenuItem("Paste Into", click: OnPasteRows, iconEmbeddedName: "paste.png");

            _renameItem = BuildMenuItem("Rename", click: OnRenameFile, iconEmbeddedName: "paste.png");

            _createFolder = BuildMenuItem("Create Folder...", click: OnCreateFolderClick,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.create-folder.png", iconAssembly: assembly);

            _upload = BuildMenuItem("Upload", iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.upload.png",
                iconAssembly: assembly);
            _upload.Items.Add(BuildMenuItem("File...", click: UploadFileClick,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.file_up.png", iconAssembly: assembly));
            _upload.Items.Add(BuildMenuItem("Folder...", click: UploadDirectoryClick,
                iconEmbeddedName: "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder_up.png", iconAssembly: assembly));

            _restoreItem = BuildMenuItem("Initiate Restore From Glacier...", click: OnRestore,
                tooltip: "Restore object(s) that have been transitioned to Amazon Glacier");

            _invokeLambda = BuildMenuItem("Invoke Lambda Function...", click: OnInvokeLambda,
                tooltip: "Invokes a Lambda function with object keys as arguments to the function");

            _changeStorageClass = BuildMenuItem("Change Storage Class");
            foreach (var storageClass in StorageClass.StorageClasses)
            {
                _changeStorageClass.Items.Add(BuildMenuItem(storageClass.Name, (sender, e) => ChangeStorageClass(storageClass.S3StorageClass)));
            }

            _changeServerSideEncryption = BuildMenuItem("Change Encryption");
            _changeServerSideEncryption.Items.Add(BuildMenuItem("None", click: OnNoEncryptionClick));
            _changeServerSideEncryption.Items.Add(BuildMenuItem("AES-256", click: OnAes256Click));

            _cloudFrontInvalidation = BuildMenuItem("CloudFront Invalidate", click: OnRequestInvalidationClick,
                iconEmbeddedName: AwsImageResourcePath.CloudFrontDownloadDistribution.Path, iconAssembly: typeof(AwsImageResourcePath).Assembly);

            _generatePreSignedUrl = BuildMenuItem("Create Pre-Signed URL...", click: OnGeneratePreSignedUrl);
        }

        private void OnGridContextMenu(object sender, RoutedEventArgs e)
        {
            BuildMenuItems();

            var selectedItems = GetSelectedItemsAsList();
            var notOnlyGlacierItemsSelected = selectedItems.Any(x => x.StorageClass == null || x.StorageClass.Value != S3StorageClass.Glacier);

            _makePublicItem.IsEnabled = selectedItems.Count > 0;
            _openItem.IsEnabled = selectedItems.Count > 0;
            _downloadItem.IsEnabled = selectedItems.Count > 0;
            _propertiesItem.IsEnabled = selectedItems.Count == 1;
            _generatePreSignedUrl.IsEnabled = selectedItems.Count == 1;
            _deleteItem.IsEnabled = selectedItems.Count > 0;
            _renameItem.IsEnabled = selectedItems.Count == 1 && notOnlyGlacierItemsSelected;

            _copyItem.IsEnabled = selectedItems.Count > 0 && notOnlyGlacierItemsSelected;
            _cutItem.IsEnabled = selectedItems.Count > 0 && notOnlyGlacierItemsSelected;
            _pasteItem.IsEnabled = _controller.ClipboardContainer.Clipboard != null || System.Windows.Clipboard.ContainsFileDropList();
            _pasteIntoItem.IsEnabled = _controller.ClipboardContainer.Clipboard != null || System.Windows.Clipboard.ContainsFileDropList();
            _restoreItem.IsEnabled = selectedItems.Any(x => x.StorageClass == S3StorageClass.Glacier || x.ChildType == BucketBrowserModel.ChildType.Folder);

            _invokeLambda.IsEnabled = selectedItems.Count > 0 && notOnlyGlacierItemsSelected;

            _changeStorageClass.IsEnabled = notOnlyGlacierItemsSelected;
            _changeServerSideEncryption.IsEnabled = notOnlyGlacierItemsSelected;

            var commonMenuItems = new List<MenuItem>();
            var secondaryMenuItems = new List<MenuItem> { _createFolder, _upload };
            var clipboardMenuItems = new List<MenuItem>();
            var settingsMenuItems = new List<MenuItem>();
            var commandsMenuItems = new List<MenuItem>();
            var destructiveMenuItems = new List<MenuItem>();
            var propertiesMenuItems = new List<MenuItem>();

            if (selectedItems.Count > 0)
            {
                commonMenuItems.Add(_downloadItem);
                clipboardMenuItems.Add(_cutItem);
                clipboardMenuItems.Add(_copyItem);
                settingsMenuItems.Add(_changeStorageClass);
                settingsMenuItems.Add(_changeServerSideEncryption);
                commandsMenuItems.Add(_makePublicItem);
                commandsMenuItems.Add(_invokeLambda);
                commandsMenuItems.Add(_restoreItem);
                destructiveMenuItems.Add(_deleteItem);

                if (selectedItems.Count == 1)
                {
                    BucketBrowserModel.ChildType childType = selectedItems[0].ChildType;

                    if (childType == BucketBrowserModel.ChildType.File)
                    {
                        commonMenuItems.Insert(0, _openItem);
                        AddCopyToClipboardMenuItems(selectedItems[0], clipboardMenuItems);
                        commandsMenuItems.Add(_generatePreSignedUrl);
                        destructiveMenuItems.Add(_renameItem);
                        propertiesMenuItems.Add(_propertiesItem);
                    }

                    if (childType == BucketBrowserModel.ChildType.Folder)
                    {
                        _pasteIntoItem.Uid = selectedItems[0].FullPath;
                        clipboardMenuItems.Add(_pasteIntoItem);
                    }
                    else
                    {
                        _pasteItem.Uid = _model.Path;
                        clipboardMenuItems.Add(_pasteItem);
                    }
                }

                if (_controller.CloudFrontDistributions.Count > 0)
                {
                    commandsMenuItems.Add(_cloudFrontInvalidation);
                }
            }

            // Build the menu, adding separators between each group
            var contextMenu = new ContextMenu();
            foreach (var menuItems in new [] { commonMenuItems, secondaryMenuItems, clipboardMenuItems,
                         settingsMenuItems, commandsMenuItems, destructiveMenuItems, propertiesMenuItems })
            {
                if (menuItems.Count == 0)
                {
                    continue;
                }

                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                }

                menuItems.ForEach(menuItem => contextMenu.Items.Add(menuItem));
            }
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
        }

        private void CopyUrlToClipboard(BucketBrowserModel.ChildItem item)
        {
            try
            {
                var url = _controller.S3Client.GetPublicURL(_controller.Model.BucketName, item.FullPath);
                System.Windows.Clipboard.SetText(url.ToString());
                _controller.RecordCopyUrlMetric(Result.Succeeded, false);
            }
            catch (Exception ex)
            {
                _logger.Error("Error copying URL to clipboard", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error copying URL to clipboard: {ex.Message}");
                _controller.RecordCopyUrlMetric(Result.Failed, false);
            }
        }

        private void AddCopyToClipboardMenuItems(BucketBrowserModel.ChildItem item, List<MenuItem> clipboardMenuItems)
        {
            clipboardMenuItems.Add(BuildMenuItem("Copy URL to Clipboard", click: (sender, e) => CopyUrlToClipboard(item)));

            if (_controller.CloudFrontDistributions.Count > 0)
            {
                var copyMenuItem = BuildMenuItem("Copy Distribution URLs");

                foreach (var dis in _controller.CloudFrontDistributions)
                {
                    if (dis.Aliases.Items.Count > 0)
                    {
                        foreach (var cname in dis.Aliases.Items)
                        {
                            copyMenuItem.Items.Add(CreateCopyToDistributionMenuItem(cname, item.FullPath));
                        }
                    }
                    else
                    {
                        copyMenuItem.Items.Add(CreateCopyToDistributionMenuItem(dis.DomainName, item.FullPath));
                    }
                }

                clipboardMenuItems.Add(copyMenuItem);
            }
        }

        private MenuItem CreateCopyToDistributionMenuItem(string domain, string path)
        {
            var header = $"http://{domain}/{path}";
            return BuildMenuItem(header, click: (sender, e) =>
            {
                try
                {
                    System.Windows.Clipboard.SetText(header);
                }
                catch (Exception ex)
                {
                    _logger.Error("Error copying URL to clipboard", ex);
                    ToolkitFactory.Instance.ShellProvider.ShowError($"Error copying URL to clipboard: {ex.Message}");
                }
            });
        }

        #endregion

        #region ContextMenu Callback(s)

        private void OnRestore(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count > 0)
                {
                    var controller = new RestoreObjectPromptController(_controller.S3Client);
                    if (controller.Execute() && int.TryParse(controller.Model.RestoreDays, out var days))
                    {
                        _ctlJobTracker.AddJob(new RestoreObjectsJob(_controller, selectedItems, days));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error restoring object(s)", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error restoring object(s): {ex.Message}");
            }
        }

        private void OnInvokeLambda(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count > 0)
                {
                    new InvokeLambdaFunctionController(_controller.S3Client, Model.BucketName,
                        _controller.GetListOfKeys(selectedItems, false), _controller.RegionProvider).Execute();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error invoking Lambda function", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error invoking Lambda function(s): {ex.Message}");
            }
        }

        private void OnGeneratePreSignedUrl(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
                {
                    _controller.GeneratePreSignedURL(selectedItems[0].FullPath);
                    _controller.RecordCopyUrlMetric(Result.Succeeded, true);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error generating pre-signed URL", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error generating pre-signed URL: {ex.Message}");
                _controller.RecordCopyUrlMetric(Result.Failed, true);
            }
        }

        private void OnOpen(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
                {
                    _controller.Open(selectedItems[0].FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error opening file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error opening file: {ex.Message}");
            }
        }

        private void OnPropertiesRows(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
                {
                    _controller.ShowObjectProperties(selectedItems[0]);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error displaying properties", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error displaying properties: {ex.Message}");
            }
        }

        private void OnRenameFile(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItems = GetSelectedItemsAsList();
                if (selectedItems?.Count == 1 && selectedItems[0].ChildType == BucketBrowserModel.ChildType.File)
                {
                    _controller.Rename(selectedItems[0]);
                    SortGrid();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error renaming file", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error renaming file: {ex.Message}");
            }
        }

        private void OnDeleteRows(object sender, RoutedEventArgs e)
        {
            try
            {
                var childItems = new List<BucketBrowserModel.ChildItem>();
                foreach (BucketBrowserModel.ChildItem childItem in _ctlDataGrid.SelectedItems)
                {
                    if (childItem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
                    {
                        continue;
                    }

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

                var confirmMsg = $"Are you sure you want to permanently delete \"{(childItems.Count == 1 ? childItems[0].Title : childItems.Count + " items")}\"?";
                if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Items?", confirmMsg))
                {
                    _ctlJobTracker.AddJob(new DeleteFilesJob(_controller, childItems));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error deleting", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting: {ex.Message}");
            }
        }

        private void OnDownloadRows(object sender, RoutedEventArgs e)
        {
            try
            {
                var downloadLocation = DirectoryBrowserDlgHelper.ChooseDirectory(this, "Choose a folder to download files.");
                if (!string.IsNullOrEmpty(downloadLocation))
                {
                    _ctlJobTracker.AddJob(new DownloadFilesJob(_controller, _controller.BucketName,
                        _model.Path, GetSelectedItemsAsList().ToArray(), downloadLocation));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error downloading", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error downloading: {ex.Message}");
            }
        }

        private void OnMakePublicRows(object sender, RoutedEventArgs e)
        {
            try
            {
                _ctlJobTracker.AddJob(new MakePublicJob(_controller, GetSelectedItemsAsList()));
            }
            catch (Exception ex)
            {
                _logger.Error("Error making public", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error making public: {ex.Message}");
            }
        }

        private void CutOrCopyRows(S3Clipboard.ClipboardMode mode)
        {
            _controller.ClipboardContainer.Clipboard = new S3Clipboard(mode, _controller, _controller.Model.Path, GetSelectedItemsAsList());
            System.Windows.Clipboard.Clear();
        }

        private void OnCutRows(object sender, RoutedEventArgs e)
        {
            CutOrCopyRows(S3Clipboard.ClipboardMode.Cut);
        }

        private void OnCopyRows(object sender, RoutedEventArgs e)
        {
            CutOrCopyRows(S3Clipboard.ClipboardMode.Copy);
        }

        private void OnPasteRows(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Clipboard.GetDataObject()?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                OnFileDrop(System.Windows.Clipboard.GetDataObject());
                return;
            }

            if (_controller.ClipboardContainer.Clipboard == null)
            {
                return;
            }

            try
            {
                var job = new PasteFilesJob(_controller, _controller.ClipboardContainer.Clipboard,
                    sender is MenuItem item ? item.Uid : _controller.Model.Path);

                if (_controller.ClipboardContainer.Clipboard.Mode == S3Clipboard.ClipboardMode.Cut)
                {
                    _controller.ClipboardContainer.Clipboard = null;
                }

                _ctlJobTracker.AddJob(job);
            }
            catch (Exception ex)
            {
                _logger.Error("Error pasting", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError($"Error pasting: {ex.Message}");
            }
        }

        private List<BucketBrowserModel.ChildItem> GetSelectedItemsAsList()
        {
            var itemsInClipboard = new List<BucketBrowserModel.ChildItem>();
            foreach (BucketBrowserModel.ChildItem selectedItem in _ctlDataGrid.SelectedItems)
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

        private void OnFileDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent("FileDrop") ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            OnFileDrop(e.Data);
        }

        private void OnFileDrop(IDataObject data)
        {
            try
            {
                string[] dataItems = data.GetData(DataFormats.FileDrop) as string[];
                if (dataItems == null || dataItems.Length == 0)
                {
                    return;
                }

                var files = new List<string>();
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

                var parent = File.Exists(dataItems[0]) ? new FileInfo(dataItems[0]).DirectoryName : new DirectoryInfo(dataItems[0]).Parent?.FullName;
                UploadFiles(files.ToArray(), parent);
            }
            catch (Exception ex)
            {
                _logger.Error("Error on file drop", ex);
            }
        }

        private Point _lastMouseDown;

        private void OnDataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(_ctlDataGrid);
            }
        }

        private void OnDataGridMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && _controller.S3RootViewModel != null)
                {
                    if (UIUtils.IsPointOnScrollBar(_lastMouseDown, _ctlDataGrid))
                    {
                        return;
                    }

                    var currentPosition = e.GetPosition(_ctlDataGrid);

                    if (Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0 ||
                        Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0)
                    {
                        var selectedItems = GetSelectedItemsAsList();
                        if (selectedItems.Count > 0)
                        {
                            _ctlDataGrid.AllowDrop = false;
                            var s3DataObject = new S3DataObject(_controller.S3RootViewModel, _model.BucketName, _model.Path, selectedItems);
                            s3DataObject.SetData(DataFormats.StringFormat, s3DataObject);
                            DragDrop.DoDragDrop(_ctlDataGrid, s3DataObject, DragDropEffects.Copy);
                            _ctlDataGrid.AllowDrop = true;
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

        private void OnNewChildItems(object sender, EventArgs e)
        {
            SortGrid();
        }

        private void SortGrid()
        {
            try
            {
                var lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(_ctlDataGrid.ItemsSource);
                var comparer = lcv.CustomSort as IComparer<BucketBrowserModel.ChildItem> ?? new NameComparer(ListSortDirection.Descending);
                _controller.Model.ChildItems.SortDisplayedChildItems(comparer);
            }
            catch
            {
            }
        }

        private void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var column = e.Column;
            var direction = column.SortDirection != ListSortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;
            column.SortDirection = direction;

            IComparer comparer;
            switch (column.Header.ToString())
            {
                case "Size":
                    comparer = new SizeComparer(direction);
                    break;
                case "Last Modified Date":
                    comparer = new DateComparer(direction);
                    break;
                default:
                    comparer = new NameComparer(direction);
                    break;
            }

            // Use a ListCollectionView to do the sort.
            ((ListCollectionView)CollectionViewSource.GetDefaultView(_ctlDataGrid.ItemsSource)).CustomSort = comparer;
        }

        #endregion

        #region Grid Shortcut Key(s)

        private void CopyCommandExecuted(object sender, ExecutedRoutedEventArgs e) 
        {
            OnCopyRows(sender, e);
        }

        private void CutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
           OnCutRows(sender, e);
        }

        private void PasteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OnPasteRows(sender, e);
        }

        private void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateUpDirectory();
        }

        private void NavigateUpDirectory()
        {
            if (_ctlBreadCrumbPanel.Children.Count < 4)
            {
                return;
            }

            // Subtract 3 from the end for the current label, angle bracket and image.
            var previous = _ctlBreadCrumbPanel.Children[_ctlBreadCrumbPanel.Children.Count - 4];
            UpdateBreadCrumb(previous.Uid);
        }

        private void OnGridKeyDown(object sender, KeyEventArgs e)
        {
            var selectedItems = GetSelectedItemsAsList();

            if (e.Key == Key.Delete && selectedItems.Count > 0)
            {
                OnDeleteRows(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (_ctlDataGrid.SelectedItems.Count == 1)
                {
                    RowMouseDoubleClick(sender, null);
                    e.Handled = true;
                }
            }
        }

        #endregion

        private void OnCancelLoad(object sender, MouseButtonEventArgs e)
        {
            _controller.CancelFetchingObjects();
        }

        private bool _isOnCancelHandlerAdded;

        public void UpdateFetchingStatus(string message, bool enableCancel)
        {
            _ctlFetchStatus.Text = message;

            if (enableCancel)
            {
                if (!_isOnCancelHandlerAdded)
                {
                    _ctlFetchCancel.MouseLeftButtonDown += OnCancelLoad;
                    _isOnCancelHandlerAdded = true;
                }
                _ctlFetchCancel.Cursor = Cursors.Hand;
                _ctlFetchCancel.Foreground = FindResource("awsUrlTextFieldForegroundBrushKey") as SolidColorBrush;
            }
            else
            {
                if (_isOnCancelHandlerAdded)
                {
                    _ctlFetchCancel.MouseLeftButtonDown -= OnCancelLoad;
                    _isOnCancelHandlerAdded = false;
                }
                _ctlFetchCancel.Cursor = Cursors.Arrow;
                _ctlFetchCancel.Foreground = FindResource("awsDisabledControlForegroundBrushKey") as SolidColorBrush; 
            }
        }
    }
}
