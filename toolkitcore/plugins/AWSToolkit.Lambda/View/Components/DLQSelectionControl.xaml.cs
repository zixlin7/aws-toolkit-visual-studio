using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for DLQSelectionControl.xaml
    /// </summary>
    public partial class DLQSelectionControl : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DLQSelectionControl()
        {
            InitializeComponent();
        }


        public void SetAvailableDLQTargets(IList<string> topicArns, IList<string> queueArns, string defaultTargetArn)
        {
            DisplayItem selectedItem = null;
            Func<string, string> nameFromArn = x =>
            {
                var pos = x.LastIndexOf(':');
                return x.Substring(pos + 1);
            };

            var items = new List<DisplayItem>();

            items.Add(new DisplayItem
            {
                Name = "<no dead letter queue>"
            });

            foreach (var topicArn in topicArns)
            {
                var item = new DisplayItem
                {
                    Name = nameFromArn(topicArn),
                    Category = "SNS",
                    Arn = topicArn
                };

                if (string.Equals(topicArn, defaultTargetArn, StringComparison.Ordinal))
                {
                    selectedItem = item;
                }
                items.Add(item);
            }

            foreach (var queueArn in queueArns)
            {
                var item = new DisplayItem
                {
                    Name = nameFromArn(queueArn),
                    Category = "SQS",
                    Arn = queueArn
                };

                if (string.Equals(queueArn, defaultTargetArn, StringComparison.Ordinal))
                {
                    selectedItem = item;
                }
                items.Add(item);
            }

            ListCollectionView lcv = new ListCollectionView(items);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            this._ctlDLQOptions.ItemsSource = lcv;
            this._ctlDLQOptions.SelectedItem = selectedItem ?? items[0];
        }

        public string SelectedArn
        {
            get
            {
                var item = this._ctlDLQOptions.SelectedItem as DisplayItem;
                if (item == null)
                    return null;

                return item.Arn;
            }
        }



        private void _ctlDLQOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DLQTargets"));
        }

        public class DisplayItem
        {
            public string Name { get; set; }
            public string Arn { get; set; }
            public string Category { get; set; }

            public override string ToString()
            {
                return this.Name;
            }
        }
    }
}
