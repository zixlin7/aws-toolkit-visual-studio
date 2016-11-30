using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.CloudFront.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class InvalidationSummaryWrapper : BaseModel
    {
        InvalidationSummary _summary;
        DateTime? _createTime;

        public InvalidationSummaryWrapper(InvalidationSummary summary)
        {
            this._summary = summary;
        }

        public string Id
        {
            get { return this._summary.Id; }
        }

        public string Status
        {
            get { return this._summary.Status; }
        }

        public string DisplayName
        {
            get { return string.Format("{0} ({1})", this.Id, this.Status); }
        }

        public DateTime? CreateTime
        {
            get { return this._createTime; }
            set
            {
                this._createTime = value;
                base.NotifyPropertyChanged("CreateTime");
            }
        }

        ObservableCollection<InvalidationPath> _paths;
        public ObservableCollection<InvalidationPath> Paths
        {
            get { return this._paths; }
            set
            {
                this._paths = value;
                base.NotifyPropertyChanged("Paths");
            }
        }

        public class InvalidationPath
        {
            public InvalidationPath(string path)
            {
                this.Path = path;
            }

            public string Path
            {
                get;
                private set;
            }

            public object Icon
            {
                get
                {
                    Image image = IconHelper.GetIconByExtension(this.Path);

                    if (image == null)
                        image = IconHelper.GetIcon("Amazon.AWSToolkit.Resources.generic-file.png");

                    return image.Source;
                }
            }
        }
    }
}
