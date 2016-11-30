using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Amazon.AWSToolkit.CommonUI;

using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Model
{
    public class BucketBrowserModel : BaseModel
    {
        public event EventHandler NewItems;

        public enum ChildType { File, Folder, LinkToParent};

        string _bucketName;
        string _path = "";
        ChildItem _selectedItem;
        ChildItemCollection _childItemCollection;

        public BucketBrowserModel(string bucketName)
        {
            this._bucketName = bucketName;
            this._childItemCollection = new ChildItemCollection(this);
        }

        bool _loading;
        public bool Loading
        {
            get { return this._loading; }
            set
            {
                this._loading = value;
                base.NotifyPropertyChanged("Loading");
            }
        }

        public string BucketName
        {
            get { return this._bucketName; }
        }

        public string Path
        {
            get { return this._path; }
            set 
            { 
                this._path = value;
                base.NotifyPropertyChanged("Path");
            }
        }

        public ChildItem SelectedItem
        {
            get { return this._selectedItem; }
            set
            {
                this._selectedItem = value;
                base.NotifyPropertyChanged("SelectedItem");
            }
        }


        string _textFilter;
        public string TextFilter
        {
            get { return this._textFilter; }
            set
            {
                this._textFilter = value;
                base.NotifyPropertyChanged("TextFilter");
            }
        }

        void raiseNewItemEvent()
        {
            if (NewItems != null && !this.Loading)
                NewItems(this, new EventArgs());
        }

        public ChildItemCollection ChildItems
        {
            get { return this._childItemCollection; }
        }

        public class ChildItemCollection
        {
            BucketBrowserModel _model;
            Dictionary<string, ChildItem> _fullListByPath = new Dictionary<string, ChildItem>();
            ObservableCollection<ChildItem> _displayedChildItems = new ObservableCollection<ChildItem>();

            public ChildItemCollection(BucketBrowserModel model)
            {
                this._model = model;
            }

            public ObservableCollection<ChildItem> DisplayedChildItems
            {
                get
                {
                    return this._displayedChildItems;
                }
            }

            public int LoadItemsCount
            {
                get { return this._fullListByPath.Count; }
            }

            public void SortDisplayedChildItems(IComparer<BucketBrowserModel.ChildItem> comparer)
            {
                List<BucketBrowserModel.ChildItem> sorted = this.DisplayedChildItems.ToList();
                sorted.Sort(comparer);


                // If we only have a small amount of items try to just rearrange this list so we don't lose selection or anything.
                if (this.DisplayedChildItems.Count < 500)
                {
                    for (int i = 0; i < sorted.Count(); i++)
                    {
                        this.DisplayedChildItems.Move(this.DisplayedChildItems.IndexOf(sorted[i]), i);
                    }
                }
                else
                {
                    this.DisplayedChildItems.Clear();
                    foreach (var item in sorted)
                        this.DisplayedChildItems.Add(item);
                }
            }

            public void Add(List<ChildItem> childItems)
            {
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    this._model.Loading = true;
                    try
                    {
                        foreach (var childItem in childItems)
                        {
                            this.Add(childItem);
                        }
                    }
                    finally
                    {
                        this._model.Loading = false;
                        this._model.raiseNewItemEvent();
                    }
                }));
            }

            public void Add(ChildItem childItem)
            {
                // Check to see if the item being added is in a subfolder from the current path.
                // If it is then we don't want to add the file but we do want to make
                // sure the folder has been added.
                if (childItem.ParentPath != this._model.Path)
                {
                    if (childItem.FullPath.StartsWith(this._model.Path))
                    {
                        string temp = childItem.FullPath.Substring(this._model.Path.Length);
                        if (temp.StartsWith("/"))
                            temp = temp.Substring(1);

                        int pos = temp.IndexOf('/');
                        if (pos < 0)
                        {
                            return;
                        }

                        string foldername = temp.Substring(0, pos);
                        if(!string.IsNullOrEmpty(this._model.Path))
                            foldername = this._model.Path + "/" + foldername;
                        ChildItem folder = new ChildItem(foldername, ChildType.Folder);
                        Add(folder);
                    }
                    return;
                }

                // If a file is being re added then just update the size and last modified date.
                if (this._fullListByPath.ContainsKey(childItem.FullPath))
                {
                    if (childItem.ChildType == ChildType.File)
                    {
                        var existing = this._fullListByPath[childItem.FullPath];
                        existing.Size = childItem.Size;
                        existing.LastModifiedDate = childItem.LastModifiedDate;                        
                    }
                }
                else
                {
                    this._fullListByPath.Add(childItem.FullPath, childItem);

                    if (childItem.PassClientFilter(this._model.TextFilter))
                    {
                        this._displayedChildItems.Add(childItem);
                    }

                    this._model.raiseNewItemEvent();
                }
            }

            public void Remove(HashSet<ChildItem> childItems)
            {
                foreach (var itemToRemove in childItems)
                {
                    this._fullListByPath.Remove(itemToRemove.FullPath);
                }

                List<int> indexes = new List<int>();
                int index = 0;
                foreach (var item in this.DisplayedChildItems)
                {
                    if (childItems.Contains(item))
                    {
                        indexes.Add(index);
                    }
                    index++;
                }

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    for (int i = indexes.Count - 1; i >= 0; i--)
                    {
                        index = indexes[i];
                        this.DisplayedChildItems.RemoveAt(index);
                    }
                }));
            }

            public void Clear()
            {
                this._fullListByPath.Clear();
                this._displayedChildItems.Clear();

                var link = BucketBrowserModel.ChildItem.CreateLinkToParent();
                this._fullListByPath.Add(link.FullPath, link);
                this._displayedChildItems.Add(link);
            }

            public void ReApplyFilter()
            {
                this._displayedChildItems.Clear();

                foreach (var childItem in this._fullListByPath.Values)
                {
                    if (childItem.ChildType == ChildType.LinkToParent || 
                        childItem.PassClientFilter(this._model.TextFilter))
                    {
                        this._displayedChildItems.Add(childItem);
                    }
                }
            }
        }

        public class ChildItem : BaseModel
        {
            string _title;
            string _fullPath;
            long _size;
            DateTime? _lastModifiedDate;
            ChildType _childType;
            S3StorageClass _storageClass;

            public ChildItem(string fullPath, ChildType childType)
            {
                this._fullPath = fullPath;
                this._childType = childType;
            }

            public ChildItem(string fullPath, ChildType childType, long size, DateTime? lastModifiedDate, S3StorageClass storageClass)
                : this(fullPath, size, lastModifiedDate.GetValueOrDefault(), storageClass)
            {
                this._childType = childType;
            }

            public ChildItem(string fullPath, long size, string lastModifiedDate, S3StorageClass storageClass)
            {
                DateTime dt;
                if(DateTime.TryParse(lastModifiedDate, out dt))
                    initializeFile(fullPath, size, dt, storageClass);
                else
                    initializeFile(fullPath, size, null, storageClass);
            }

            public ChildItem(string fullPath, long size, DateTime lastModifiedDate, S3StorageClass storageClass)
            {
                initializeFile(fullPath, size, lastModifiedDate, storageClass);
            }

            void initializeFile(string fullPath, long size, DateTime? lastModifiedDate, S3StorageClass storageClass)
            {
                this._fullPath = fullPath;
                this._size = size;

                this._lastModifiedDate = lastModifiedDate;
                this.StorageClass = storageClass;
                this._childType = ChildType.File;
            }

            public static ChildItem CreateLinkToParent()
            {
                return new ChildItem("..", ChildType.LinkToParent);
            }

            public string Title
            {
                get 
                {
                    if (this._title == null)
                    {
                        int pos = this._fullPath.LastIndexOf('/', this._fullPath.Length - 1);
                        if (pos > 0)
                            this._title = this._fullPath.Substring(pos + 1);
                        else
                            this._title = this._fullPath;
                    }
                    return this._title; 
                }
            }

            public string FullPath
            {
                get
                {
                    return this._fullPath;
                }
                set
                {
                    this._title = null;
                    this._fullPath = value;
                    this.NotifyPropertyChanged("FullPath");
                    this.NotifyPropertyChanged("Title");
                    this.NotifyPropertyChanged("Icon");
                }
            }

            public string ParentPath
            {
                get
                {
                    int pos = this.FullPath.LastIndexOf('/');
                    if (pos <= 0)
                        return string.Empty;

                    return this.FullPath.Substring(0, pos);
                }
            }

            public S3StorageClass StorageClass
            {
                get { return this._storageClass; }
                set
                {
                    this._storageClass = value;
                    this.NotifyPropertyChanged("StorageClass");
                    this.NotifyPropertyChanged("FormattedStorageClass");
                }
            }

            public string FormattedStorageClass
            {
                get 
                {
                    if (this._storageClass == null)
                        return "";

                    return this._storageClass.ToString(); 
                }
            }

            public ChildType ChildType
            {
                get
                {
                    return this._childType;
                }
            }

            public object Icon
            {
                get
                {
                    Image image;
                    if (this._childType == ChildType.Folder)
                    {
                        image = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder.png");
                    }
                    else if (this._childType == ChildType.LinkToParent)
                    {
                        image = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.LinkToParentDirectory.png");
                    }
                    else
                        image = IconHelper.GetIconByExtension(this.FullPath);

                    if(image == null)
                        image = IconHelper.GetIcon("Amazon.AWSToolkit.Resources.generic-file.png");

                    return image.Source;
                }
            }

            public string FormattedSize
            {
                get
                {
                    if (this._childType == ChildType.File)
                        return this._size.ToString("#,0") + " bytes";

                    return "--"; 
                }
            }

            public DateTime? FormattedLastModifiedDate
            {
                get
                {
                    if (this._childType == ChildType.File)
                        return this._lastModifiedDate;

                    return null;
                }
            }

            public long Size
            {
                get { return this._size; }
                set
                {
                    this._size = value;
                    this.NotifyPropertyChanged("Size");
                    this.NotifyPropertyChanged("FormattedSize");
                }
            }

            public DateTime? LastModifiedDate
            {
                get { return this._lastModifiedDate; }
                set
                {
                    this._lastModifiedDate = value;
                    this.NotifyPropertyChanged("LastModifiedDate");
                    this.NotifyPropertyChanged("FormattedLastModifiedDate");
                }
            }

            public bool PassClientFilter(string filter)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    string textFilter = filter.ToLower();
                    if (this.Title.ToString().ToLower().Contains(textFilter))
                        return true;
                    if (this.FormattedLastModifiedDate.ToString().ToLower().Contains(textFilter))
                        return true;
                    if (this.FormattedSize.ToString().ToLower().Contains(textFilter))
                        return true;

                    return false;
                }

                return true;
            }
        }
   }
}
