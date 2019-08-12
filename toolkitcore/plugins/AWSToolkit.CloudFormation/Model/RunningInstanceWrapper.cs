using System;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class RunningInstanceWrapper : BaseModel
    {
        Reservation _reservation;
        Instance _instance;

        public RunningInstanceWrapper(Reservation reservation, Instance instance)
        {
            this._reservation = reservation;
            this._instance = instance;
        }

        public string InstanceId => this._instance.InstanceId;

        public string ImageId => this._instance.ImageId;

        public string Status => this._instance.State.Name;

        public string ReservationId => this._reservation.ReservationId;

        public string Platform => this._instance.Platform;

        public bool IsWindowsPlatform => EC2Constants.PLATFORM_WINDOWS.Equals(this.NativeInstance.Platform, StringComparison.OrdinalIgnoreCase);

        public string Name
        {
            get
            {
                string name = string.Empty;
                var tag = this._instance.Tags.Find(item => item.Key.Equals(EC2Constants.TAG_NAME));
                if (tag != null && !string.IsNullOrEmpty(tag.Value))
                {
                    name = tag.Value;
                }
                return name;
            }
        }

        public string InstanceType
        {
            get => this.NativeInstance.InstanceType;
            set
            {
                this.NativeInstance.InstanceType = value;
                base.NotifyPropertyChanged("InstanceType");
            }
        }

        public string IpAddress => this._instance.PublicIpAddress;

        public string FormattedTags
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var tag in this._instance.Tags)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    sb.AppendFormat("{0}={1}", tag.Key, tag.Value);
                }
                return sb.ToString();
            }
        }

        public Reservation NativeReservation => this._reservation;

        public Instance NativeInstance => this._instance;

        public System.Windows.Media.ImageSource InstanceIcon
        {
            get
            {
                var iconPath = IsWindowsPlatform ? "instance-windows.gif" : "instance-generic.png";
                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                switch (this.NativeInstance.State.Name)
                {
                    case EC2Constants.INSTANCE_STATE_RUNNING:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.INSTANCE_STATE_TERMINATED:
                    case EC2Constants.INSTANCE_STATE_STOPPED:
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
    }
}
