using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
