using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Clipboard;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.S3.IO;
using Amazon.CloudFront.Model;

using log4net;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class BucketBrowserController : BaseContextCommand
    {
        static ILog _logger = LogManager.GetLogger(typeof(BucketBrowserController));

        IAmazonS3 _s3Client;
        string _bucketName;
        BucketBrowserControl _control;
        BucketBrowserModel _browserModel;
        Dispatcher _uiDispatcher;
        IS3ClipboardContainer _clipboardContainer = new DefaultS3ClipboardContainer();
        S3RootViewModel _s3RootViewModel;
        Thread _loadingThread;

        public BucketBrowserController()
        {
        }

        public BucketBrowserController(IAmazonS3 s3Client, BucketBrowserModel model)
        {
            this._s3Client = s3Client;
            this._browserModel = model;
            this._bucketName = model.BucketName;
        }


        public override ActionResults Execute(IViewModel model)
        {
            S3BucketViewModel bucketModel = model as S3BucketViewModel;
            if (bucketModel == null)
                return new ActionResults().WithSuccess(false);

            this._s3RootViewModel = bucketModel.S3RootViewModel;
            // Make sure the manifest watcher has started so DnD to explorer will work.
            ManifestWatcher.Instance.Start(); 
            this._clipboardContainer = bucketModel.S3RootViewModel;
            return Execute(bucketModel.S3Client, new BucketBrowserModel(bucketModel.Name));
        }

        public ActionResults Execute(IAmazonS3 s3Client, BucketBrowserModel bucketModel)
        {
            this._s3Client = s3Client;
            this._bucketName = bucketModel.BucketName;
            this._browserModel = bucketModel;
            this._browserModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onBrowserModelPropertyChanged);

            this._control = new BucketBrowserControl(this, this._browserModel);
            this._uiDispatcher = this._control.Dispatcher;

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        void onBrowserModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Path"))
            {
                StartFetchingObject();
            }            
        }

        public BucketBrowserModel Model => this._browserModel;

        public string BucketName => this._bucketName;

        public IAmazonS3 S3Client => this._s3Client;

        public S3RootViewModel S3RootViewModel => this._s3RootViewModel;

        public IS3ClipboardContainer ClipboardContainer => this._clipboardContainer;

        public void Refresh()
        {
            try
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    StartFetchingObject();
                })); 
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing S3 bucket model", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading folders and files: " + e.Message);
            }
        }

        public void AddChildItemsToModel(List<BucketBrowserModel.ChildItem> childItems)
        {
            this._browserModel.ChildItems.Add(childItems);
        }

        public void RemoveChildItemsFromModel(List<BucketBrowserModel.ChildItem> childItems)
        {
            HashSet<BucketBrowserModel.ChildItem> hash = new HashSet<BucketBrowserModel.ChildItem>();
            foreach (var item in childItems)
                hash.Add(item);
            RemoveChildItemsFromModel(hash);
        }

        public void RemoveChildItemsFromModel(HashSet<BucketBrowserModel.ChildItem> childItems)
        {
            this._browserModel.ChildItems.Remove(childItems);
        }

        public void DisposeLoadingThread()
        {
            if (this._loadingThread != null && this._loadingThread.IsAlive)
                this._loadingThread.Abort();
        }

        public void CancelFetchingObjects()
        {
            DisposeLoadingThread();
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this._control.UpdateFetchingStatus(
                    string.Format("Cancelled Fetching After {0} Items", this._browserModel.ChildItems.LoadItemsCount - 1), 
                    false);
            }));
        }

        public void StartFetchingObject()
        {
            DisposeLoadingThread();

            this._loadingThread = new Thread(new ThreadStart(this.fetchObjects));
            this._loadingThread.Start();
        }


        void fetchObjects()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this._browserModel.ChildItems.Clear();
                this._browserModel.SelectedItem = null;
                this._control.UpdateFetchingStatus("Fetching 0 Items", true);
            }));


            bool firstPass = true;
            string nextMarker = null;
            try
            {
                do
                {
                    string[] directories;
                    S3Object[] files;
                    S3Directory.GetDirectoriesAndFiles(this._s3Client, this.BucketName, this._browserModel.Path, out directories, out files, ref nextMarker);

                    List<BucketBrowserModel.ChildItem> items = new List<BucketBrowserModel.ChildItem>();
                    foreach (string directory in directories)
                    {
                        BucketBrowserModel.ChildItem item = new BucketBrowserModel.ChildItem(directory, BucketBrowserModel.ChildType.Folder);
                        items.Add(item);
                    }

                    foreach (S3Object file in files)
                    {
                        BucketBrowserModel.ChildItem item = new BucketBrowserModel.ChildItem(file.Key, file.Size, file.LastModified, file.StorageClass);
                        items.Add(item);
                    }

                    var itemsArray = items.ToArray();
                    Array.Sort(itemsArray, new NameComparer(ListSortDirection.Descending));

                    var localNextMarker = nextMarker;
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                    {
                        this._browserModel.Loading = true;
                        try
                        {
                            foreach (var item in itemsArray)
                                this._browserModel.ChildItems.Add(item);

                            // For the first page selected a row and put the data grid in focus to allow keyboard navigation.
                            if (firstPass)
                            {
                                if (this._browserModel.ChildItems.DisplayedChildItems.Count > 0)
                                {
                                    this._browserModel.SelectedItem = this._browserModel.ChildItems.DisplayedChildItems[0];
                                }

                                this._control.SetDataGridFocus();
                            }

                            string message = string.Format("{0} {1} Items",
                                string.IsNullOrEmpty(localNextMarker as string) ? "Fetched" : "Fetching",
                                this._browserModel.ChildItems.LoadItemsCount - 1);  // Minus 1 to remove the ".." link to parent folder
                            this._control.UpdateFetchingStatus(message, !string.IsNullOrEmpty(localNextMarker as string));
                        }
                        finally
                        {
                            this._browserModel.Loading = false;
                        }
                    }));

                    firstPass = false;
                }
                while (!string.IsNullOrEmpty(nextMarker));
            }
            catch (Exception e)
            {
                _logger.Error("Error fetching object names for bucket " + this._bucketName, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error fetching object names: " + e.Message);
            }

            this._loadingThread = null;
        }

        public List<string> GetListOfKeys(IEnumerable<BucketBrowserModel.ChildItem> childItems, bool includeFolderFiles)
        {
            return GetListOfKeys(childItems, includeFolderFiles, null);
        }

        public List<string> GetListOfKeys(IEnumerable<BucketBrowserModel.ChildItem> childItems, bool includeFolderFiles, HashSet<S3StorageClass> validStorageClasses)
        {
            try
            {
                List<string> keys = new List<string>();

                foreach (BucketBrowserModel.ChildItem childItem in childItems)
                {
                    if (childItem.ChildType == BucketBrowserModel.ChildType.File)
                    {
                        if (validStorageClasses == null || validStorageClasses.Contains(childItem.StorageClass.Value))
                            keys.Add(childItem.FullPath);
                    }
                    else
                    {
                        string[] files = S3Directory.GetFiles(this._s3Client, this._bucketName, childItem.FullPath, SearchOption.AllDirectories, includeFolderFiles, validStorageClasses);
                        foreach (var file in files)
                        {
                            keys.Add(file);
                        }
                    }
                }

                return keys;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting list of keys: " + e.Message);
                return new List<string>();
            }
        }

        public void Rename(BucketBrowserModel.ChildItem childItem)
        {
            RenameFileController controller = new RenameFileController(this.S3Client, this.BucketName, childItem.FullPath);
            if (controller.Execute())
            {
                childItem.FullPath = controller.Model.NewFullPathKey;
            }
        }

        public void CreateFolder()
        {
            NewFolderController controller = new NewFolderController(this.S3Client,
                this.BucketName, this._browserModel.Path);
            if (controller.Execute())
            {
                string folderPath;
                if (string.IsNullOrEmpty(this._browserModel.Path))
                    folderPath = controller.Model.NewFolderName;
                else
                    folderPath = this._browserModel.Path + "/" + controller.Model.NewFolderName;

                var childItem = new BucketBrowserModel.ChildItem(folderPath, BucketBrowserModel.ChildType.Folder);
                this._browserModel.ChildItems.Add(childItem);
            }
        }

        public void Open(string key)
        {
            var oldS3SigV4 = AWSConfigsS3.UseSignatureVersion4;
            try
            {
                AWSConfigsS3.UseSignatureVersion4 = true;
                string url = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = this._bucketName,
                    Key = key,
                    Expires = DateTime.Now.AddMinutes(15)
                });

                Process.Start(new ProcessStartInfo(url));
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening file: " + e.Message);
            }
            finally
            {
                AWSConfigsS3.UseSignatureVersion4 = oldS3SigV4;
            }
        }

        public void GeneratePreSignedURL(string key)
        {
            var controller = new CreatePresignedURLController();
            controller.Execute(this._s3Client, this._bucketName, key);
        }

        public void ShowObjectProperties(BucketBrowserModel.ChildItem selectedItem)
        {
            try
            {
                ObjectPropertiesController controller = new ObjectPropertiesController(this._s3Client, this.BucketName, selectedItem.FullPath);
                controller.Execute();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating folder: " + e.Message);
            }
        }

        public S3AccessControlList GetBucketAccessList()
        {
            var request = new GetACLRequest() { BucketName = this._bucketName };
            var response = this._s3Client.GetACL(request);
            return response.AccessControlList;
        }

        public void PerformInvalidationRequest(List<BucketBrowserModel.ChildItem> selectedItems)
        {
            List<string> keys = GetListOfKeys(selectedItems, false);
            foreach (ICloudFrontDistributionViewModel cfViewModel in connectedCloudFrontDistributions())
            {
                var postRequest = new CreateInvalidationRequest();
                postRequest.DistributionId = cfViewModel.DistributionId;
                postRequest.InvalidationBatch = new InvalidationBatch();
                postRequest.InvalidationBatch.CallerReference = Guid.NewGuid().ToString();
                postRequest.InvalidationBatch.Paths = new Paths();
                foreach(var key in keys)
                {
                    string formmattedKey;
                    if (key.StartsWith("/"))
                        formmattedKey = key;
                    else
                        formmattedKey = "/" + key;

                    postRequest.InvalidationBatch.Paths.Items.Add(formmattedKey);
                }
                postRequest.InvalidationBatch.Paths.Quantity = postRequest.InvalidationBatch.Paths.Items.Count;
                cfViewModel.CFClient.CreateInvalidation(postRequest);
            }
        }

        public IList<ICloudFrontDistributionViewModel> CloudFrontDistributions => connectedCloudFrontDistributions();

        private IList<ICloudFrontDistributionViewModel> connectedCloudFrontDistributions()
        {
            var list = new List<ICloudFrontDistributionViewModel>();
            var cfRootModel = this._s3RootViewModel.AccountViewModel.FindSingleChild<ICloudFrontRootViewModel>(false);
            if (cfRootModel != null)
            {
                foreach (IViewModel cfModel in cfRootModel.Children)
                {
                    if (cfModel is ICloudFrontDistributionViewModel)
                    {
                        var distributionViewModel = cfModel as ICloudFrontDistributionViewModel;
                        foreach (var origin in distributionViewModel.Origins.Items)
                        {
                            if (!origin.DomainName.Contains(".s3."))
                                continue;

                            string targetBucket = origin.DomainName;
                            int pos = targetBucket.IndexOf(".s3.amazonaws.com");
                            if (pos > 0)
                            {
                                targetBucket = targetBucket.Substring(0, pos);
                            }

                            if (this._bucketName.Equals(targetBucket))
                            {
                                list.Add(distributionViewModel);
                            }
                        }
                    }
                }
            }
            return list;
        }

        class DefaultS3ClipboardContainer : IS3ClipboardContainer
        {
            public S3Clipboard Clipboard
            {
                get;
                set;
            }
        }
    }
}
