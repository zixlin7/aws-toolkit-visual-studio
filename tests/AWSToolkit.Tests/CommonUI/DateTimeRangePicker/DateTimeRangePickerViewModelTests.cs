using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.DateTimeRangePicker;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.DateTimeRangePicker
{
    public class DateTimeRangePickerViewModelTests
    {
        private readonly DateTimeRangePickerViewModel _viewModel = new DateTimeRangePickerViewModel(null, null);

        [Fact]
        public void DateUpdatesDefaultTime()
        {
            Assert.Null(_viewModel.StartTimeModel.TimeInput);

            _viewModel.StartDate = new DateTime(2022, 05, 06);

            Assert.Equal(default(DateTime).ToLongTimeString(), _viewModel.StartTimeModel.TimeInput);
        }

        public static readonly IEnumerable<object[]> InvalidDateUpdateTimeData = new[]
        {
            new object[] { null, null },
            new object[] { null, "" },
            new object[] { null, "11:20:30 PM" },
            new object[] { new DateTime(2022, 05, 04), "11:20:30 PM" },
        };

        [Theory]
        [MemberData(nameof(InvalidDateUpdateTimeData))]
        public void DateDoesNotUpdateTime(DateTime? date, string timeInput)
        {
            _viewModel.EndDate = date;
            _viewModel.EndTimeModel.TimeInput = timeInput;

            Assert.NotEqual(default(DateTime).ToLongTimeString(), _viewModel.EndTimeModel.TimeInput);
        }

        public static readonly IEnumerable<object[]> ErrorData = new[]
        {
            new object[]
            {
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"),
                new DateTime(2022, 05, 03), GetTimeInLocalFormat("11:20:30 PM"), true
            },
            new object[]
            {
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"),
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:29 PM"), true
            },
            new object[] { null, null,
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"), false },
            new object[] { new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"),
                null, null, false },
            new object[]
            {
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"),
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:31 PM"), false
            },
            new object[]
            {
                new DateTime(2022, 05, 04), GetTimeInLocalFormat("11:20:30 PM"),
                new DateTime(2022, 05, 05), GetTimeInLocalFormat("11:20:30 PM"), false
            }
        };

        [Theory]
        [MemberData(nameof(ErrorData))]
        public void HasError(DateTime? startDate, string startTime, DateTime? endDate, string endTime,
            bool expectedHasError)
        {
            _viewModel.StartDate = startDate;
            _viewModel.EndDate = endDate;
          
            _viewModel.StartTimeModel.TimeInput = startTime;
            _viewModel.StartTimeModel.SetTime();

            _viewModel.EndTimeModel.TimeInput = endTime;
            _viewModel.EndTimeModel.SetTime();

            _viewModel.RefreshRange();

            Assert.Equal(expectedHasError, _viewModel.HasErrors);
        }

        [Fact]
        public void DateRaisesRangeChangedEvent()
        {
            var receivedEvent = Assert.Raises<EventArgs>(
                a => _viewModel.RangeChanged += a,
                a => _viewModel.RangeChanged -= a,
                () => _viewModel.EndDate = new DateTime(2022, 05, 04));

            Assert.NotNull(receivedEvent);

            Assert.Equal(_viewModel.EndDate, _viewModel.GetFullEndTime());
        }

        [Fact]
        public void RefreshRange()
        {
            _viewModel.EndDate = new DateTime(2022, 05, 04);
            Assert.Equal(_viewModel.EndDate, _viewModel.GetFullEndTime());

            _viewModel.EndTimeModel.TimeInput = GetTimeInLocalFormat("11:20:30 PM");
            _viewModel.EndTimeModel.SetTime();

            var receivedEvent = Assert.Raises<EventArgs>(
                a => _viewModel.RangeChanged += a,
                a => _viewModel.RangeChanged -= a,
                () => _viewModel.RefreshRange());


            Assert.NotNull(receivedEvent);
            var expected = _viewModel.EndDate?.Date.Add(_viewModel.EndTimeModel.Time.TimeOfDay);
            Assert.Equal(expected, _viewModel.GetFullEndTime());
        }

        [Fact]
        public void RemoveCommand_CannotExecute()
        {
            Assert.False(_viewModel.RemoveCommand.CanExecute(null));
        }

        public static readonly IEnumerable<object[]> RemoveCanExecuteData = new[]
        {
            new object[] { new DateTime(2022, 05, 04), null },
            new object[] { null, new DateTime(2022, 05, 04) }
        };

        [Theory]
        [MemberData(nameof(RemoveCanExecuteData))]
        public void RemoveCommand_CanExecute(DateTime? startDate, DateTime? endDate)
        {
            _viewModel.StartDate = startDate;
            _viewModel.EndDate = endDate;

            Assert.True(_viewModel.RemoveCommand.CanExecute(null));
        }

        [Fact]
        public void RemoveCommand_Execute()
        {
            _viewModel.StartDate = new DateTime(2022, 05, 04);
            _viewModel.EndDate = new DateTime(2022, 05, 06);

            _viewModel.RemoveCommand.Execute(null);

            Assert.Null(_viewModel.StartDate);
            Assert.Null(_viewModel.EndDate);
            Assert.Null(_viewModel.StartTimeModel.TimeInput);
            Assert.Null(_viewModel.EndTimeModel.TimeInput);
        }

        private static string GetTimeInLocalFormat(string time)
        {
            var format = DateTimeUtil.GetLocalSystemFormat().LongTimePattern;
            return DateTime.Parse(time).ToString(format);
        }
    }
}
