using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using System.Windows.Controls;

namespace Amazon.AWSToolkit.Shared
{
    public interface IAWSToolkitControl : INotifyPropertyChanged
    {
        string Title { get; }
        string UniqueId { get; }
        string MetricId { get; }

        UserControl UserControl { get; }

        bool SupportsBackGroundDataLoad { get; }
        void ExecuteBackGroundLoadDataLoad();

        bool Validated();
        bool OnCommit();

        void RefreshInitialData(object initialData);
        object GetInitialData();
    }
}
