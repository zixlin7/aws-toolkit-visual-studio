using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewVolumesModel : BaseModel
    {
        ObservableCollection<VolumeWrapper> _volumes = new ObservableCollection<VolumeWrapper>();
        public ObservableCollection<VolumeWrapper> Volumes => _volumes;

        IList<VolumeWrapper> _selectedVolumes = new List<VolumeWrapper>();
        public IList<VolumeWrapper> SelectedVolumes => _selectedVolumes;

        VolumeWrapper _focusVolume;
        public VolumeWrapper FocusVolume
        {
            get => _focusVolume;
            set
            {
                _focusVolume = value;
                base.NotifyPropertyChanged("FocusVolume");
            }
        }

        EC2ColumnDefinition[] _volumePropertytColumnDefinitions;
        public EC2ColumnDefinition[] VolumePropertyColumnDefinitions
        {
            get
            {
                if (this._volumePropertytColumnDefinitions == null)
                {
                    this._volumePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(VolumeWrapper));
                }

                return this._volumePropertytColumnDefinitions;
            }
        }

        EC2ColumnDefinition[] _snapshotPropertytColumnDefinitions;
        public EC2ColumnDefinition[] SnapshotPropertyColumnDefinitions
        {
            get
            {
                if (this._snapshotPropertytColumnDefinitions == null)
                {
                    this._snapshotPropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(SnapshotWrapper));
                }

                return this._snapshotPropertytColumnDefinitions;
            }
        }


        public string[] ListVolumeAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.Volumes);
    }
}
