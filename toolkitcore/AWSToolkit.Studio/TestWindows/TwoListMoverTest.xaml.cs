using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Amazon.AWSToolkit.Studio.TestWindows
{
    /// <summary>
    /// Interaction logic for TwoListMoverTest.xaml
    /// </summary>
    public partial class TwoListMoverTest : Window
    {
        ObservableCollection<object> _available = new ObservableCollection<object>();
        ObservableCollection<object> _assigned = new ObservableCollection<object>();

        public TwoListMoverTest()
        {
            //for (int i = 1; i < 500; i++)
            //{
            //    _available.Add("Available" + i);
            //    _assigned.Add("Assigned" + i);
            //}
            _available.Add("Available3");
            _available.Add("Available2");
            _available.Add("Available4");
            _available.Add("Available1");

            _assigned.Add("Assigned2");
            _assigned.Add("Assigned1");
            _assigned.Add("Assigned3");
            
            InitializeComponent();
        }

        public ObservableCollection<object> Available
        {
            get { return this._available; }
            set { this._available = value; }
        }

        public ObservableCollection<object> Assigned
        {
            get { return this._assigned; }
            set { this._assigned = value; }
        }

    }
}
