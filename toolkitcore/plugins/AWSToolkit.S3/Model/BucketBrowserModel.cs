using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;

using Amazon.S3;
using Amazon.S3.IO;

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
            get => this._loading;
            set
            {
                this._loading = value;
                base.NotifyPropertyChanged("Loading");
            }
        }

        public string BucketName => this._bucketName;

        public string Path
        {
            get => this._path;
            set 
            { 
                this._path = value;
                base.NotifyPropertyChanged("Path");
            }
        }

        public ChildItem SelectedItem
        {
            get => this._selectedItem;
            set
            {
                this._selectedItem = value;
                base.NotifyPropertyChanged("SelectedItem");
            }
        }


        string _textFilter;
        public string TextFilter
        {
            get => this._textFilter;
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

        public ChildItemCollection ChildItems => this._childItemCollection;

        public class ChildItemCollection
        {
            BucketBrowserModel _model;
            Dictionary<string, ChildItem> _fullListByPath = new Dictionary<string, ChildItem>();
            ObservableCollection<ChildItem> _displayedChildItems = new ObservableCollection<ChildItem>();

            public ChildItemCollection(BucketBrowserModel model)
            {
                this._model = model;
            }

            public ObservableCollection<ChildItem> DisplayedChildItems => this._displayedChildItems;

            public int LoadItemsCount => this._fullListByPath.Count;

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
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
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
                if (childItem.ParentPath != _model.Path)
                {
                    if (S3Path.IsDescendant(_model.Path, childItem.ParentPath))
                    {
                        // Strip off _model.Path, get the first directory name, then add _model.Path back
                        var subfolder = S3Path.Combine(
                            _model.Path,
                            S3Path.GetFirstNonRootPathComponent(
                                S3Path.GetRelativePath(_model.Path, childItem.FullPath)
                            )
                        );

                        Add(new ChildItem(subfolder, ChildType.Folder));
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

                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
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
            private string _fullPath;
            private long _size;
            private DateTime? _lastModifiedDate;
            private S3StorageClass _storageClass;

            public ChildItem(string fullPath, ChildType childType)
            {
                FullPath = fullPath;
                ChildType = childType;
            }

            public ChildItem(string fullPath, ChildType childType, long size, DateTime? lastModifiedDate, S3StorageClass storageClass)
                : this(fullPath, size, lastModifiedDate.GetValueOrDefault(), storageClass)
            {
                ChildType = childType;
            }

            public ChildItem(string fullPath, long size, string lastModifiedDate, S3StorageClass storageClass)
            {
                if (DateTime.TryParse(lastModifiedDate, out var dt))
                {
                    InitializeFile(fullPath, size, dt, storageClass);
                }
                else
                {
                    InitializeFile(fullPath, size, null, storageClass);
                }
            }

            public ChildItem(string fullPath, long size, DateTime lastModifiedDate, S3StorageClass storageClass)
            {
                InitializeFile(fullPath, size, lastModifiedDate, storageClass);
            }

            private void InitializeFile(string fullPath, long size, DateTime? lastModifiedDate, S3StorageClass storageClass)
            {
                FullPath = fullPath;
                Size = size;

                LastModifiedDate = lastModifiedDate;
                StorageClass = storageClass;
                ChildType = ChildType.File;
            }

            public static ChildItem CreateLinkToParent()
            {
                return new ChildItem("..", ChildType.LinkToParent);
            }

            public string FullPath
            {
                get => _fullPath;
                set
                {
                    _fullPath = value;
                    Title = S3Path.GetLastPathComponent(_fullPath);
                    ParentPath = S3Path.GetParentPath(_fullPath);
                    NotifyPropertyChanged(nameof(FullPath));
                    NotifyPropertyChanged(nameof(Title));
                    NotifyPropertyChanged(nameof(ParentPath));
                    NotifyPropertyChanged(nameof(Icon));
                }
            }

            public string Title { get; private set; }

            public string ParentPath { get; private set; }

            public S3StorageClass StorageClass
            {
                get => _storageClass;
                set
                {
                    _storageClass = value;
                    NotifyPropertyChanged(nameof(StorageClass));
                    NotifyPropertyChanged(nameof(FormattedStorageClass));
                }
            }

            public string FormattedStorageClass => _storageClass?.ToString() ?? string.Empty;

            public ChildType ChildType { get; private set; }

            public object Icon
            {
                get
                {
                    Image image;
                    if (ChildType == ChildType.Folder)
                    {
                        image = IconHelper.GetIcon(GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.folder.png");
                    }
                    else if (ChildType == ChildType.LinkToParent)
                    {
                        image = IconHelper.GetIcon(GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.LinkToParentDirectory.png");
                    }
                    else
                    {
                        image = IconHelper.GetIconByExtension(FullPath);
                    }

                    if (image == null)
                    {
                        image = IconHelper.GetIcon("Amazon.AWSToolkit.Resources.generic-file.png");
                    }

                    return image.Source;
                }
            }

            public string FormattedSize => ChildType == ChildType.File ? Size.ToString("#,0") + " bytes" : "--";

            public DateTime? FormattedLastModifiedDate => ChildType == ChildType.File ? LastModifiedDate : null;

            public long Size
            {
                get => _size;
                set
                {
                    _size = value;
                    NotifyPropertyChanged(nameof(Size));
                    NotifyPropertyChanged(nameof(FormattedSize));
                }
            }

            public DateTime? LastModifiedDate
            {
                get => _lastModifiedDate;
                set
                {
                    _lastModifiedDate = value;
                    NotifyPropertyChanged(nameof(LastModifiedDate));
                    NotifyPropertyChanged(nameof(FormattedLastModifiedDate));
                }
            }

            public bool PassClientFilter(string filter)
            {
                if (string.IsNullOrEmpty(filter))
                {
                    return true;
                }

                string textFilter = filter.ToLower();
                return
                    Title.ToLower().Contains(textFilter) ||
                    FormattedLastModifiedDate.ToString().ToLower().Contains(textFilter) ||
                    FormattedSize.ToLower().Contains(textFilter);
            }
        }
   }
}
