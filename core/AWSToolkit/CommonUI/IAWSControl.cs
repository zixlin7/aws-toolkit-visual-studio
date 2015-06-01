﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI
{
    public interface IAWSControl : INotifyPropertyChanged
    {
        string Title { get; }
        string UniqueId { get; }
        UserControl UserControl { get; }

        bool SupportsBackGroundDataLoad { get; }
        void ExecuteBackGroundLoadDataLoad();

        bool Validated();
        bool OnCommit();

        void RefreshInitialData(object initialData);
        object GetInitialData();
    }
}
