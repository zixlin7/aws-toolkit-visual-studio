using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class StackEventWrapper
    {
        StackEvent _event;

        public StackEventWrapper(StackEvent evnt)
        {
            this._event = evnt;
        }

        public StackEvent NativeStackEvent => this._event;


        public System.Windows.Media.ImageSource StatusImage
        {
            get
            {
                string iconPath;
                if (this.NativeStackEvent.ResourceStatus.Value.EndsWith("IN_PROGRESS"))
                    iconPath = "yellow-circle.png";
                else if (this.NativeStackEvent.ResourceStatus.Value.Equals("DELETE_COMPLETE"))
                    iconPath = "blue-circle.png";
                else if (this.NativeStackEvent.ResourceStatus.Value.EndsWith("_COMPLETE"))
                    iconPath = "green-circle.png";
                else if (this.NativeStackEvent.ResourceStatus.Value.EndsWith("_FAILED"))
                    iconPath = "red-circle.png";
                else
                    iconPath = "green-circle.png";


                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        public SolidColorBrush SeverityColor
        {
            get
            {
                Color clr;
                switch (this.NativeStackEvent.ResourceStatus)
                {
                    case "CREATE_FAILED":
                    case "DELETE_FAILED":
                        clr = Colors.Red;
                        break;

                    default:
                        clr = Colors.Green;
                        break;
                }
                return new SolidColorBrush(clr);
            }
        }

        public bool PassClientFilter(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                string textFilter = filter.ToLower();
                if (this.NativeStackEvent.Timestamp.ToString().ToLower().Contains(textFilter))
                    return true;
                if (this.NativeStackEvent.ResourceType != null && this.NativeStackEvent.ResourceType.ToLower().Contains(textFilter))
                    return true;
                if (this.NativeStackEvent.LogicalResourceId != null && this.NativeStackEvent.LogicalResourceId.ToLower().Contains(textFilter))
                    return true;
                if (this.NativeStackEvent.PhysicalResourceId != null && this.NativeStackEvent.PhysicalResourceId.ToLower().Contains(textFilter))
                    return true;
                if (this.NativeStackEvent.ResourceStatus != null && this.NativeStackEvent.ResourceStatus.Value.ToLower().Contains(textFilter))
                    return true;
                if (this.NativeStackEvent.ResourceStatusReason != null && this.NativeStackEvent.ResourceStatusReason.ToLower().Contains(textFilter))
                    return true;

                return false;
            }

            return true;
        }
    }
}
