using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CommonUI.DateTimeRangePicker
{
    /// <summary>
    /// Backing model representing a Date Time Range Picker
    /// </summary>
    public class DateTimeRangePickerViewModel : BaseModel
    {
        private DateTime? _startDate;
        private DateTime? _endDate;
        private readonly TimeInputViewModel _startTimeViewModel;
        private readonly TimeInputViewModel _endTimeViewModel;
        private ICommand _clearCommand;
        private bool _hasErrors = false;
        public event EventHandler<EventArgs> RangeChanged;

        public DateTimeRangePickerViewModel(DateTime? startTime, DateTime? endTime)
        {
            var format = DateTimeUtil.GetLocalSystemFormat().LongTimePattern;
            _startTimeViewModel = new TimeInputViewModel { Format = format };
            _endTimeViewModel = new TimeInputViewModel { Format = format };

            _startTimeViewModel.SetTimeInput(startTime);
            _endTimeViewModel.SetTimeInput(endTime);

            StartDate = startTime?.Date;
            EndDate = endTime?.Date;
        }

        public TimeInputViewModel StartTimeModel => _startTimeViewModel;
        public TimeInputViewModel EndTimeModel => _endTimeViewModel;

        public ICommand ClearCommand => _clearCommand ?? (_clearCommand = CreateClearCommand());

        private ICommand CreateClearCommand()
        {
            return new RelayCommand(CanExecuteClear, Clear);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);
                UpdateDefaultTime(StartDate, StartTimeModel);
                RefreshRange();
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);
                UpdateDefaultTime(EndDate, EndTimeModel);
                RefreshRange();
            }
        }

        public DateTime? GetFullEndTime()
        {
            return EndDate?.Date.Add(EndTimeModel.Time.TimeOfDay);
        }

        public DateTime? GetFullStartTime()
        {
            return StartDate?.Date.Add(StartTimeModel.Time.TimeOfDay);
        }


        public bool HasErrors
        {
            get => _hasErrors;
            set => SetProperty(ref _hasErrors, value);
        }

        /// <summary>
        /// Refresh and validate if range is valid. If yes, raise range changed event
        /// </summary>
        public void RefreshRange()
        {
            if (IsValid())
            {
                RaiseRangeChanged();
            }
        }

        /// <summary>
        /// Initializes corresponding time field associated with the date to a default value(12:00:00 AM) if none present
        /// </summary>
        private void UpdateDefaultTime(DateTime? date, TimeInputViewModel timeModel)
        {
            if (date != null && string.IsNullOrWhiteSpace(timeModel.TimeInput))
            {
                timeModel.SetTimeInput(default(DateTime));
            }
        }

        /// <summary>
        /// Checks if the date time values selected represent a valid range
        /// </summary>
        private bool IsValid()
        {
            var startFullTime = GetFullStartTime();
            var endFullTime = GetFullEndTime();
            var result = startFullTime != null && endFullTime != null && startFullTime >= endFullTime;

            HasErrors = result;
            return !result;
        }

        private bool CanExecuteClear(object arg)
        {
            return GetFullEndTime() != null || GetFullStartTime() != null;
        }

        /// <summary>
        /// Clears out all date time selections made
        /// </summary>
        private void Clear(object obj)
        {
            StartDate = null;
            EndDate = null;
            _startTimeViewModel.TimeInput = null;
            _endTimeViewModel.TimeInput = null;
        }

        private void RaiseRangeChanged()
        {
            RangeChanged?.Invoke(this, new EventArgs());
        }
    }
}
