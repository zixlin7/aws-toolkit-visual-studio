using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AMIImage = Amazon.EC2.Model.Image;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Control used to display a list of quick-launch AMIs
    /// </summary>
    public partial class QuickLaunchAMIListControl : INotifyPropertyChanged
    {
        public static readonly string uiProperty_Ami = "Ami";

        readonly ObservableCollection<EC2QuickLaunchImage> _displayedImages = new ObservableCollection<EC2QuickLaunchImage>();
        IEnumerable<EC2QuickLaunchImage> _allImages; 

        public enum PlatformType
        {
            All,
            Windows,
            Linux
        }

        public QuickLaunchAMIListControl()
        {
            InitializeComponent();
            _amiList.ItemsSource = _displayedImages;
        }

        PlatformType _platformType;
        public PlatformType PlatformFilter 
        {
            set
            {
                _platformType = value;
                switch (_platformType)
                {
                    case PlatformType.All:
                        _allPlatforms.IsChecked = true;
                        break;
                    case PlatformType.Linux:
                        _linuxOnly.IsChecked = true;
                        break;
                    case PlatformType.Windows:
                        _windowsOnly.IsChecked = true;
                        break;
                }

                if (_allImages != null)
                    RenderImagesForPlatformFilter();
            }
        }

        public IEnumerable<EC2QuickLaunchImage> Images 
        {
            set
            {
                _allImages = value;
                RenderImagesForPlatformFilter();
           }
        }

        public string SelectedAMIID
        {
            get
            {
                if (IsInitialized)
                {
                    var image = this._amiList.SelectedItem as EC2QuickLaunchImage;
                    if (image != null)
                    {
                        if (image.Is32BitSelected)
                            return image.ImageId32;
                        else
                            return image.ImageId64;
                    }
                }

                return string.Empty;
            }
        }

        public EC2QuickLaunchImage SelectedAMI
        {
            get
            {
                if (IsInitialized)
                    return this._amiList.SelectedItem as EC2QuickLaunchImage;
                else
                    return null;
            }
            set
            {
                this._amiList.SelectedItem = value;
            }
        }

        public bool HasSelection
        {
            get { return this._amiList.SelectedItem != null; }
        }

        public bool AllowFiltering
        {
            get
            {
                return _filterControls.Visibility == Visibility.Visible;
            }
            set
            {
                _filterControls.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void RenderImagesForPlatformFilter()
        {
            _displayedImages.Clear();
            if (_allImages == null)
                return;

            foreach (EC2QuickLaunchImage img in _allImages)
            {
                if (_platformType == PlatformType.All || !this.AllowFiltering)
                    _displayedImages.Add(img);
                else
                {
                    if (_platformType == PlatformType.Windows)
                    {
                        if (img.IsWindowsPlatform)
                            _displayedImages.Add(img);
                    }
                    else
                    {
                        if (!img.IsWindowsPlatform)
                        {
                            if (img.Title.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
                            {
                                _displayedImages.Insert(0, img);
                            }
                            else
                            {
                                _displayedImages.Add(img);
                            }
                        }
                    }
                }
            }
        }

        private void _amiList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Ami);
        }

        private void PlatformFilter_Click(object sender, RoutedEventArgs e)
        {
            RadioButton btn = sender as RadioButton;
            string platformTypeName = btn.Tag as string;
            PlatformFilter = (PlatformType)Enum.Parse(typeof(PlatformType), platformTypeName, true);
        }
    }
}
