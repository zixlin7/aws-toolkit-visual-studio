using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI;

using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.EC2.Model
{
    /// <summary>
    /// Wrapper around ami data for an image in the EC2 quick launch set
    /// </summary>
    /// <remarks>
    /// {  'virtualizationType': 'paravirtual', 
    ///    'description': 'Amazon Linux AMI Base 2011.02.1, EBS boot, 32-bit architecture with Amazon EC2 AMI Tools.', 
    ///    'title': 'Basic 32-bit Amazon Linux AMI 2011.02.1 Beta', 
    ///    'imageId': 'ami-8c1fece5', 
    ///    'platform': 'amazon', 
    ///    'architecture': 'i386', 
    ///    'rootDeviceType': 'ebs', 
    ///    'position': 0, 
    ///    'freeTier': True, 
    ///    'totalImageSize': 8, 
    ///    'ports': [22] } 
    /// </remarks>
    public class EC2QuickLaunchImage : INotifyPropertyChanged
    {
        public static readonly string uiProperty_Bitness = "bitness";

        const string jsonkeyVirtualizationType = "virtualizationType";
        const string jsonkeyDescription = "description";
        const string jsonkeyTitle = "title";
        const string jsonkeyImageId32 = "imageId32";
        const string jsonkeyImageId64 = "imageId64";
        const string jsonkeyPlatform = "platform";
        const string jsonkeyArchitecture = "architecture";
        const string jsonkeyRootDeviceType = "rootDeviceType";
        const string jsonkeyPosition = "position";
        const string jsonkeyFreeTier = "freeTier";
        const string jsonkeyTotalImageSize = "totalImageSize";
        const string jsonkeyPorts = "ports";

        public static EC2QuickLaunchImage Deserialize(JsonData obj)
        {
            EC2QuickLaunchImage img = new EC2QuickLaunchImage();

            if (obj[jsonkeyVirtualizationType] != null)
                img.VirtualizationType = (string)obj[jsonkeyVirtualizationType];

            if (obj[jsonkeyDescription] != null)
                img.Description = (string)obj[jsonkeyDescription];

            if (obj[jsonkeyTitle] != null)
                img.Title = (string)obj[jsonkeyTitle];

            if (obj[jsonkeyImageId32] != null)
                img.ImageId32 = (string)obj[jsonkeyImageId32];

            if (obj[jsonkeyImageId64] != null)
                img.ImageId64 = (string)obj[jsonkeyImageId64];

            if (obj[jsonkeyPlatform] != null)
                img.Platform = (string)obj[jsonkeyPlatform];

            if (obj[jsonkeyArchitecture] != null)
                img.Architecture = (string)obj[jsonkeyArchitecture];

            if (obj[jsonkeyRootDeviceType] != null)
                img.RootDeviceType = (string)obj[jsonkeyRootDeviceType];

            if (obj[jsonkeyPosition] != null)
                img.Position = (int)obj[jsonkeyPosition];

            if (obj[jsonkeyFreeTier] != null)
            {
                if (obj[jsonkeyFreeTier].IsString)
                {
                    img.FreeTier = Boolean.Parse((string)obj[jsonkeyFreeTier]);
                }
                else if (obj[jsonkeyFreeTier].IsBoolean)
                {
                    img.FreeTier = (bool)obj[jsonkeyFreeTier];
                }
            }

            if (obj[jsonkeyTotalImageSize] != null)
                img.TotalImageSize = (int)obj[jsonkeyTotalImageSize];

            if (obj[jsonkeyPorts] != null)
            {
                JsonData ports = obj[jsonkeyPorts];
                img.Ports = new int[ports.Count];
                for (int i = 0; i < ports.Count; i++)
                {
                    img.Ports[i] = (int)ports[i];
                }
            }

            if (!string.IsNullOrEmpty(img.ImageId64))
                img.Is64BitSelected = true;
            else
                img.Is32BitSelected = true;

            return img;
        }

        public static EC2QuickLaunchImage FromImage(Amazon.EC2.Model.Image image)
        {
            EC2QuickLaunchImage img = new EC2QuickLaunchImage();
            img.Title = image.Name;
            if (!string.IsNullOrEmpty(image.Description))
                img.Description = image.Description;
            else
                img.Description = "(no description available)";

            img.Architecture = image.Architecture;
            img.Platform = image.Platform;
            img.Ports = new int[0];

            if (img.Architecture == "i386")
            {
                img.ImageId32 = image.ImageId;
                img.Is32BitSelected = true;
            }
            else
            {
                img.ImageId64 = image.ImageId;
                img.Is64BitSelected = true;
            }

            return img;
        }

        public string VirtualizationType { get; internal set; }
        public string Description { get; internal set; }
        public string Title { get; internal set; }
        public string ImageId32 { get; internal set; }
        public string ImageId64 { get; internal set; }
        public string Platform { get; internal set; }
        public string Architecture { get; internal set; }
        public string RootDeviceType { get; internal set; }
        public int Position { get; internal set; }
        public bool FreeTier { get; internal set; }
        public int TotalImageSize { get; internal set; }
        public int[] Ports { get; internal set; }

        bool _is64BitSelected;
        public bool Is64BitSelected 
        {
            get { return this._is64BitSelected; }
            set
            {
                this._is64BitSelected = value;
                NotifyPropertyChanged(uiProperty_Bitness);

            }
        }
        public bool Is64BitEnabled { get { return this.ImageId64 != null; } }

        bool _is32BitSelected;
        public bool Is32BitSelected
        {
            get { return this._is32BitSelected; }
            set
            {
                this._is32BitSelected = value;
                NotifyPropertyChanged(uiProperty_Bitness);

            }
        }
        public bool Is32BitEnabled { get { return this.ImageId32 != null; } }

        public bool IsWindowsPlatform
        {
            // seen EC2 use 'windows' and 'Windows'
            get { return EC2Constants.PLATFORM_WINDOWS.Equals(this.Platform, StringComparison.OrdinalIgnoreCase); }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource PlatformIcon
        {
            get
            {
                // have seen both 'windows' and 'Windows' in use by EC2
                var iconPath = IsWindowsPlatform ? "instance-windows.gif" : "instance-generic.png";
                var icon = IconHelper.GetIcon(iconPath);
                if (icon == null)
                    return null;

                return icon.Source;
            }
        }

        internal EC2QuickLaunchImage() { }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
