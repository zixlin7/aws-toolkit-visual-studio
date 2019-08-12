using System.IO;
using System.ComponentModel;

namespace Amazon.AWSToolkit.CommonUI.JobTracker
{
    public interface IJob : INotifyPropertyChanged
    {
        bool CanResume { get; }
        bool IsComplete { get; set;  }
        void Cancel();
        void StartJob();
        void StartResume();
        Stream ActionIcon { get; }
        bool IsActionEnabled { get; set; }

        string Title { get; set; }
        string CurrentStatus { get; set; }

        long ProgressMin { get; set; }
        long ProgressMax { get; set; }
        long ProgressValue { get; set; }

        string ProgressToolTip { get; set; }
    }
}
