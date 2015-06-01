using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Annotations;
using log4net.Core;

namespace Amazon.AWSToolkit.CommonUI
{
    public class ToolkitDataGridWrapper : DataGrid, INotifyPropertyChanged
    {
        public class ZoomLevelItem
        {
            public double Level { get; internal set; }

            public override string ToString()
            {
                return string.Format("{0} %", Level*100);
            }
        }

        public ZoomLevelItem[] ZoomLevels { get; protected set; }

        static ToolkitDataGridWrapper()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolkitDataGridWrapper),
                   new FrameworkPropertyMetadata(typeof(DataGrid)));
        }

        static readonly Uri ShellThemeDefaultsUri 
            = new Uri("/AWSToolkit;component/Themes/DefaultTheme.xaml", 
                      UriKind.RelativeOrAbsolute);

        public ToolkitDataGridWrapper()
        {
            var themeDictionary = new ResourceDictionary {Source = ShellThemeDefaultsUri};
            this.Resources.MergedDictionaries.Insert(0, themeDictionary);

            ZoomLevels = new []
            {
                new ZoomLevelItem {Level = .2},
                new ZoomLevelItem {Level = .5},
                new ZoomLevelItem {Level = .7},
                new ZoomLevelItem {Level = 1.0},
                new ZoomLevelItem {Level = 1.5},
                new ZoomLevelItem {Level = 2.0},
                new ZoomLevelItem {Level = 4.0}
            };
        }

        private double _zoomLevel = 1.0;
        public double ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                _zoomLevel = value;
                OnPropertyChanged("ZoomLevel");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
