using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class SnapshotWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        Snapshot _snapshot;

        public SnapshotWrapper(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        [Browsable(false)]
        public Snapshot NativeSnapshot
        {
            get { return _snapshot; }
        }

        [DisplayName("Start Time")]
        public DateTime Started
        {
            get { return Convert.ToDateTime(NativeSnapshot.StartTime); }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Snapshot"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return NativeSnapshot.SnapshotId; }
        }

        [DisplayName("Snapshot ID")]
        public string SnapshotId
        {
            get { return NativeSnapshot.SnapshotId; }
        }

        [DisplayName("Owner ID")]
        public string OwnerId
        {
            get { return NativeSnapshot.OwnerId; }
        }

        [DisplayName("Volume Size")]
        public int VolumeSize
        {
            get { return NativeSnapshot.VolumeSize; }
        }

        [DisplayName("Description")]
        public string Description
        {
            get { return NativeSnapshot.Description; }
        }

        [DisplayName("Name")]
        public string Name
        {
            get 
            {
                foreach (var tag in NativeSnapshot.Tags)
                {
                    if (tag.Key.Equals(EC2Constants.TAG_NAME))
                        return tag.Value;
                }
                return String.Empty; 
            }
        }

        [DisplayName("Progress")]
        public string Progress
        {
            get { return this.NativeSnapshot.Progress; }
        }

        [DisplayName("Name")]
        [AssociatedIcon(true, "StatusIcon")]
        public string Status
        {
            get { return this.NativeSnapshot.State; }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                switch (this.NativeSnapshot.State)
                {
                    case EC2Constants.SNAPSHOT_STATUS_COMPLETED:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.SNAPSHOT_STATUS_ERROR:
                        iconPath = "red-circle.png";
                        break;
                    default:
                        iconPath = "yellow-circle.png";
                        break;
                }

                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Snapshot";
            componentName = this.SnapshotId;
        }

        public Tag FindTag(string name)
        {
            if (this.NativeSnapshot.Tags == null)
                return null;

            return this.NativeSnapshot.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeSnapshot.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeSnapshot.Tags; }
        }
    }
}
