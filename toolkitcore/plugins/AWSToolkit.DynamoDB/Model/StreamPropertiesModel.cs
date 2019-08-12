using System.Collections.Generic;
using Amazon.DynamoDBv2;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class StreamPropertiesModel : BaseModel
    {

        public StreamPropertiesModel(string tableName)
        {
            this.TableName = tableName;
        }

        string _tableName;
        public string TableName
        {
            get => this._tableName;
            set
            {
                this._tableName = value;
                base.NotifyPropertyChanged("TableName");
            }
        }


        bool _enableStream;
        public bool EnableStream
        {
            get => this._enableStream;
            set
            {
                this._enableStream = value;
                base.NotifyPropertyChanged("EnableStream");
            }
        }

        string _streamArn;
        public string StreamARN
        {
            get => this._streamArn;
            set
            {
                this._streamArn = value;
                base.NotifyPropertyChanged("StreamARN");
            }
        }

        List<ViewTypeWrapper> _allViewTypes = new List<ViewTypeWrapper>
        {
            new ViewTypeWrapper(StreamViewType.KEYS_ONLY, "Keys Only - only the key attributes of the modified item"),
            new ViewTypeWrapper(StreamViewType.NEW_IMAGE, "New Image - the entire item, as it appears after it was modified"),
            new ViewTypeWrapper(StreamViewType.OLD_IMAGE, "Old Image - the entire item, as it appeared before it was modified"),
            new ViewTypeWrapper(StreamViewType.NEW_AND_OLD_IMAGES, "New and Old Images - both the new and the old images of the item")
        };

        public List<ViewTypeWrapper> AllViewTypes => this._allViewTypes;

        public ViewTypeWrapper  FindViewType(StreamViewType viewType)
        {
            foreach(var item in _allViewTypes)
            {
                if (item != null && item.ViewType == viewType)
                    return item;
            }

            return null;
        }

        ViewTypeWrapper _selectedViewType;
        public ViewTypeWrapper SelectedViewType 
        {
            get => this._selectedViewType;
            set
            {
                this._selectedViewType = value;
                base.NotifyPropertyChanged("SelectedViewType");
            }
        }


        public class ViewTypeWrapper
        {
            public ViewTypeWrapper(StreamViewType viewType, string description)
            {
                this.ViewType = viewType;
                this.Description = description;
            }

            public StreamViewType ViewType { get; }
            public string Description { get; }
        }
    }
}
