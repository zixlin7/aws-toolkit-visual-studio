using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{

    [DefaultPropertyAttribute("Name")]
    public class ImageWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        Image _image;

        public ImageWrapper(Image image)
        {
            this._image = image;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "AMI";
            componentName = this.Name;
        }

        [Browsable(false)]
        public Image NativeImage
        {
            get { return this._image; }
        }

        [DisplayName("AMI Name")]
        [AssociatedIconAttribute(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.ami.png")]
        public string Name
        {
            get
            {
                return this._image.Name;
            }
        }

        [DisplayName("AMI ID")]
        public string ImageId
        {
            get
            {
                return this._image.ImageId;
            }
        }

        [DisplayName("Owner")]
        public string FormattedOwner
        {
            get
            {
                if (!string.IsNullOrEmpty(this.NativeImage.ImageOwnerAlias))
                    return this.NativeImage.ImageOwnerAlias;
                return this.NativeImage.OwnerId;
            }
        }

        [DisplayName("Platform")]
        [AssociatedIconAttribute(true, "PlatformIcon")]
        public string FormattedPlatform
        {
            get
            {
                if (string.IsNullOrEmpty(this.NativeImage.Platform))
                    return "Linux";
                return this.NativeImage.Platform;
            }
        }

        // ec2 api shows 'windows' as platform label but have seen 'Windows' used
        internal bool IsWindowsPlatform
        {
            get { return EC2Constants.PLATFORM_WINDOWS.Equals(this.NativeImage.Platform, StringComparison.OrdinalIgnoreCase); }
        }

        [DisplayName("Visibility")]
        [ReadOnly(true)]
        public string FormattedVisibility
        {
            get { return this.NativeImage.Public ? EC2Constants.IMAGE_VISIBILITY_PUBLIC : EC2Constants.IMAGE_VISIBILITY_PRIVATE; }
            set
            {
                this.NativeImage.Public = string.Equals(value, EC2Constants.IMAGE_VISIBILITY_PUBLIC, StringComparison.InvariantCultureIgnoreCase);
                //base.NotifyPropertyChanged("FormattedVisibility");
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource PlatformIcon
        {
            get
            {
                var iconPath = this.IsWindowsPlatform ? "instance-windows.gif" : "instance-generic.png";
                var icon = IconHelper.GetIcon(iconPath);
                return icon == null ? null : icon.Source;
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StateIcon
        {
            get
            {
                string iconPath;
                switch (NativeImage.State)
                {
                    case EC2Constants.IMAGE_STATE_AVAILABLE:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.IMAGE_STATE_PENDING:
                        iconPath = "yellow-circle.png";
                        break;
                    case EC2Constants.IMAGE_STATE_FAILED:
                        iconPath = "red-circle.png";
                        break;
                    default:
                        iconPath = null;
                        break;
                }

                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        [DisplayName("Description")]
        public string Description
        {
            get { return this._image.Description; }
        }

        [DisplayName("Source")]
        public string Source
        {
            get { return this._image.ImageLocation; }
        }

        [DisplayName("State")]
        [AssociatedIconAttribute(true, "StateIcon")]
        public string State
        {
            get { return this._image.State; }
        }

        [DisplayName("Root Device Type")]
        public string RootDeviceType
        {
            get { return this._image.RootDeviceType; }
        }

        [DisplayName("Block Devices")]
        public string BlockDevices
        {
            get 
            {
                var sb = new StringBuilder();
                foreach (var map in this._image.BlockDeviceMappings)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    if (map.Ebs != null)
                        sb.AppendFormat("{0}={1}:{2}:{3}", map.DeviceName, map.Ebs.SnapshotId, map.Ebs.VolumeSize, map.Ebs.DeleteOnTermination);
                    else
                        sb.AppendFormat("{0}", map.DeviceName);
                }

                return sb.ToString(); 
            }
        }

        [DisplayName("Virtualization")]
        public string VirtualizationType
        {
            get { return this._image.VirtualizationType; }
        }

        [DisplayName("State Reason")]
        public string StateReason
        {
            get 
            {
                if (this._image.StateReason == null)
                    return string.Empty;

                return this._image.StateReason.Message; 
            }
        }

        [DisplayName("Kernal ID")]
        public string KernalId
        {
            get { return this._image.KernelId; }
        }

        [DisplayName("Architecture")]
        public string Architecture
        {
            get { return this._image.Architecture; }
        }

        [DisplayName("Root Device")]
        public string RootDeviceName
        {
            get { return this._image.RootDeviceName; }
        }

        [DisplayName("RAM Disk ID")]
        public string RAMDiskId
        {
            get { return this._image.RamdiskId; }
        }

        [DisplayName("Image Size")]
        public string FormattedImageSize
        {
            get 
            {
                return ImageSize + " GiB"; 
            }
        }

        public int ImageSize
        {
            get
            {
                int size = 0;
                foreach (var map in this._image.BlockDeviceMappings)
                {
                    if (map.Ebs == null)
                        continue;

                    size += (int)map.Ebs.VolumeSize;
                }
                return size;
            }
        }

        [DisplayName("Product Code")]
        public string ProductCodes
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var code in this._image.ProductCodes)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    sb.Append(code.ProductCodeId);
                }

                return sb.ToString();
            }
        }


        [Browsable(false)]
        public string SortVisibilityPath
        {
            get { return this.FormattedVisibility + this.ImageId; }
        }

        [Browsable(false)]
        public string SortImageStatePath
        {
            get { return this.NativeImage.State + this.ImageId; }
        }

        [Browsable(false)]
        public string SortPlatformPath
        {
            get { return this.FormattedPlatform + this.ImageId; }
        }

        [Browsable(false)]
        public string SortRootDeviceTypePath
        {
            get { return this.NativeImage.RootDeviceType + this.ImageId; }
        }

        [Browsable(false)]
        public string SortVirtualizationTypePath
        {
            get { return this.NativeImage.VirtualizationType + this.ImageId; }
        }


        #region IWrapper Implementation

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                if (Name != null && Name.Length > 0)
                    return String.Format("{0} ({1})", Name, NativeImage.ImageId);

                return NativeImage.ImageId;
            }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Image"; }
        }
        #endregion

        public override string ToString()
        {
            string identifier;
            if (string.IsNullOrEmpty(this.Name))
                identifier = this.Name;
            else
                identifier = this.NativeImage.ImageId;

            return "AMI: " + identifier;
        }

        public Tag FindTag(string name)
        {
            if (this._image.Tags == null)
                return null;

            return this._image.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this._image.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeImage.Tags; }
        }
    }
}
