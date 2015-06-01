using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
            get { return this._image; }
            set
            {
                this._image = value;
                base.NotifyPropertyChanged("Image");
            }
        }

        bool _isPublic;
        public bool IsPublic
        {
            get { return this._isPublic; }
            set
            {
                this._isPublic = value;
                base.NotifyPropertyChanged("IsPublic");
                base.NotifyPropertyChanged("IsPrivate");
            }
        }

        public bool IsPrivate
        {
            get { return !this._isPublic; }
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
            get { return this._userIds; }
            set
            {
                this._userIds = value;
                base.NotifyPropertyChanged("UserIds");
            }
        }

        HashSet<string> _originalUserIds;
        public HashSet<string> OriginalUserIds
        {
            get { return this._originalUserIds; }
            set
            {
                this._originalUserIds = value;
            }
        }
    }
}
