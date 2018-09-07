using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using Amazon.AWSToolkit.Lambda.Model;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     UI for user to select a Lambda Function Sample Event
    /// </summary>
    public partial class SampleEventPicker : UserControl, INotifyPropertyChanged
    {
        public SampleEventPicker()
        {
            InitializeComponent();
        }

        public SampleEvent SelectedItem
        {
            get
            {
                var selectedEvent = _ctlCombo.SelectedItem as UISampleEvent;

                return selectedEvent?.SampleEvent;
            }
        }

        /// <summary>
        ///     Populates the dropdown list with given sample events.
        /// </summary>
        public void SetSampleEvents(IEnumerable<SampleEvent> sampleEvents)
        {
            var items = new List<UISampleEventItem>();

            // Group events, add them by category
            foreach (var groupedEvents in sampleEvents
                .GroupBy(evnt => string.IsNullOrWhiteSpace(evnt.Group) ? "AWS" : evnt.Group)
                .OrderBy(group => group.Key))
            {
                items.Add(new UISampleEventCategory(groupedEvents.Key));
                items.AddRange(groupedEvents
                    .OrderBy(evnt => evnt.Name)
                    .Select(evnt => new UISampleEvent(evnt)));
            }

            _ctlCombo.ItemsSource = items;
        }

        private void _ctlCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedItem");
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}