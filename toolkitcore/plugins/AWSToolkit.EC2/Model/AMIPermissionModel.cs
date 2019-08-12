using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AMIPermissionModel : BaseModel
    {
        
        public AMIPermissionModel(ImageWrapper image)
        {
            this._image = image;
        }

        ImageWrapper _image;
        public ImageWrapper Image
        {
            get => this._image;
            set
            {
                this._image = value;
                base.NotifyPropertyChanged("Image");
            }
        }

        bool _isPublic;
        public bool IsPublic
        {
            get => this._isPublic;
            set
            {
                this._isPublic = value;
                base.NotifyPropertyChanged("IsPublic");
                base.NotifyPropertyChanged("IsPrivate");
            }
        }

        public bool IsPrivate
        {
            get => !this._isPublic;
            set
            {
                this._isPublic = !value;
                base.NotifyPropertyChanged("IsPublic");
                base.NotifyPropertyChanged("IsPrivate");
            }
        }

        ObservableCollection<MutableString> _userIds;
        public ObservableCollection<MutableString> UserIds
        {
            get => this._userIds;
            set
            {
                this._userIds = value;
                base.NotifyPropertyChanged("UserIds");
            }
        }

        HashSet<string> _originalUserIds;
        public HashSet<string> OriginalUserIds
        {
            get => this._originalUserIds;
            set => this._originalUserIds = value;
        }
    }
}
