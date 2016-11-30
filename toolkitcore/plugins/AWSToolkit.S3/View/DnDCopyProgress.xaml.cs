using System;
using System.Collections.Generic;
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

using Amazon.AWSToolkit.CommonUI.JobTracker;
using Amazon.AWSToolkit.S3.Jobs;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for DnDCopyProgress.xaml
    /// </summary>
    public partial class DnDCopyProgress : Window, INotifyPropertyChanged
    {
        DownloadFilesJob _job;
        public DnDCopyProgress()
        {
            InitializeComponent();
        }

        public void FinalPrepAndShow(DownloadFilesJob job)
        {
            this.Closing += new CancelEventHandler(onClosing);
            this._job = job;
            this.DataContext = this._job;
            this._ctlCurrentFilesTransferLabel.DataContext = this;
            this._ctlNumberOfFilesLabel.DataContext = this;
            this._job.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(jobPropertyChanged);
            this.Show();
        }

        void onClosing(object sender, CancelEventArgs e)
        {
            if (!this._job.IsComplete)
                this._job.Cancel();
        }

        void jobPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this._job.IsComplete)
            {
                closeInUIThread();
            }

            if (e.PropertyName.Equals("ProgressValue") || e.PropertyName.Equals("ProgressMax"))
            {
                NotifyPropertyChanged("TotalStatus");
            }
            if (e.PropertyName.Equals("CurrentFileProgressValue") || e.PropertyName.Equals("CurrentFileProgressMax"))
            {
                NotifyPropertyChanged("CurrentFileStatus");
            }
        }

        void closeInUIThread()
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this.Close();
            }));
        }

        public string TotalStatus
        {
            get
            {
                return string.Format("{0} / {1} Files", this._job.ProgressValue, this._job.ProgressMax);
            }
        }

        public string CurrentFileStatus
        {
            get
            {
                return string.Format("{0} / {1} Bytes", this._job.CurrentFileProgressValue, this._job.CurrentFileProgressMax);
            }
        }

        private void onCancelClick(object sender, RoutedEventArgs e)
        {
            this._job.Cancel();
            closeInUIThread();
        }

        #region INotifyPropertyChange Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
