using System;
using System.Windows.Media;

using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class EventWrapper
    {
        EventDescription _originalEvent;

        public EventWrapper(EventDescription originalEvent)
        {
            this._originalEvent = originalEvent;
        }

        public string Severity => this._originalEvent.Severity;

        public SolidColorBrush SeverityColor
        {
            get
            {
                Color clr;
                switch (this.Severity)
                {
                    case "INFO":
                        clr = Colors.Green;
                        break;

                    case "WARN":
                        clr = Colors.Orange;
                        break;

                    case "ERROR":
                        clr = Colors.Red;
                        break;

                    default:
                        clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark ? Colors.White : Colors.Black;
                        break;
                }
                return new SolidColorBrush(clr);
            }
        }

        public string Message => this._originalEvent.Message;

        public string VersionLabel => this._originalEvent.VersionLabel;

        public DateTime EventDate => this._originalEvent.EventDate;

        public string EnvironmentName => this._originalEvent.EnvironmentName;

        public bool PassClientFilter(string filter, bool includeEnvironmentName)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                string textFilter = filter.ToLower();
                if (this.EventDate.ToString().ToLower().Contains(textFilter))
                    return true;
                if (this.Message != null && this.Message.ToLower().Contains(textFilter))
                    return true;
                if (this.Severity != null && this.Severity.ToLower().Contains(textFilter))
                    return true;
                if (this.VersionLabel != null && this.VersionLabel.Contains(textFilter))
                    return true;
                if (includeEnvironmentName && this.EnvironmentName != null && this.EnvironmentName.Contains(textFilter))
                    return true;

                return false;
            }

            return true;
        }
    }
}
