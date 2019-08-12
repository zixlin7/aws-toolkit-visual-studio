using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Amazon.AWSToolkit.CommonUI.JobTracker
{
    /// <summary>
    /// Interaction logic for JobTrackerControl.xaml
    /// </summary>
    public partial class JobTrackerControl
    {
        readonly ObservableCollection<IJob> _jobs = new ObservableCollection<IJob>();

        public JobTrackerControl()
        {
            InitializeComponent();
            this._ctlClearBtn.DataContext = this;
            this._ctlDataGrid.DataContext = this;
        }

        public Stream ClearCompletedIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.clear-completed.png");
                return stream;
            }
        }

        public void AddJob(IJob job)
        {
            this.Jobs.Insert(0, job);
            job.StartJob();
        }

        public ObservableCollection<IJob> Jobs => this._jobs;

        public void OnActionClick(object sender, RoutedEventArgs e)
        {
            IJob job = this._ctlDataGrid.SelectedItem as IJob;
            if (job == null)
                return;

            if (job.IsComplete && job.CanResume)
                job.StartResume();
            else
                job.Cancel();
        }

        private void ClearCompletedClick(object sender, RoutedEventArgs e)
        {
            for (int i = this._jobs.Count - 1; i >= 0; i--)
            {
                if (this.Jobs[i].IsComplete)
                {
                    this.Jobs.RemoveAt(i);
                }
            }
        }

    }
}
