using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewAMIsModel : BaseModel
    {
        ObservableCollection<ImageWrapper> _images = new ObservableCollection<ImageWrapper>();
        public ObservableCollection<ImageWrapper> Images => this._images;

        CommonImageFilters _commonImageFilter = CommonImageFilters.OWNED_BY_ME;
        public CommonImageFilters CommonImageFilter
        {
            get => this._commonImageFilter;
            set
            {
                this._commonImageFilter = value;
                base.NotifyPropertyChanged("CommonImageFilter");
            }
        }

        PlatformPicker _platformFilter = PlatformPicker.ALL_PLATFORMS;
        public PlatformPicker PlatformFilter
        {
            get => this._platformFilter;
            set
            {
                this._platformFilter = value;
                base.NotifyPropertyChanged("PlatformFilter");
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



        EC2ColumnDefinition[] _propertyColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._propertyColumnDefinitions == null)
                {
                    this._propertyColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(ImageWrapper));
                }

                return this._propertyColumnDefinitions;
            }
        }

        public string[] ListAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.Images);
    }
}
