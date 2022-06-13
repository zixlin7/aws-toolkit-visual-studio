using System;
using System.Globalization;

using Amazon.AWSToolkit.CommonUI.DateTimeRangePicker;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.DateTimeRangePicker
{
    public class TimeInputViewModelTests
    {
        private readonly TimeInputViewModel _viewModel = new TimeInputViewModel();

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("11:20:30 PM", false)]
        [InlineData("1:22:33 AM", false)]
        [InlineData("hello", true)]
        [InlineData("11:20", true)]
        [InlineData("11:20:30", true)]
        [InlineData("2799020", true)]
        [InlineData("23:58:50", true)]
        [InlineData("1:2:3 AM", true)]
        [InlineData("abc1234 ", true)]
        public void TimeInputErrorValidation_DefaultFormat(string timeInput, bool expectedHasError)
        {
            _viewModel.TimeInput = timeInput;

            Assert.Equal(expectedHasError, HasError());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("abc1234 ", true)]
        [InlineData("01:20:30", false)]
        [InlineData("21:20:30", false)]
        [InlineData("1:20:30 PM", true)]
        [InlineData("1:20:30", true)]
        [InlineData("1:20", true)]
        [InlineData("1:2:3", true)]
        [InlineData("1:2:30", true)]
        public void TimeInputErrorValidation_CustomFormat(string timeInput, bool expectedHasError)
        {
            // format - hh:mm:ss
            _viewModel.Format = CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.LongTimePattern;
            _viewModel.TimeInput = timeInput;

            Assert.Equal(expectedHasError, HasError());
        }

        [Fact]
        public void SetTimeInput()
        {
            _viewModel.Format = CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.LongTimePattern;
            var dateTime = new DateTime(2022, 05, 04, 1, 2, 30);

            _viewModel.SetTimeInput(dateTime);

            Assert.Equal("01:02:30", _viewModel.TimeInput);
        }


        [Fact]
        public void SetTime_Success()
        {
            _viewModel.Format = CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.LongTimePattern;
            _viewModel.TimeInput = "01:02:30";

            _viewModel.SetTime();

            var expected = DateTime.Parse("01:02:30");
            Assert.Equal(expected.TimeOfDay, _viewModel.Time.TimeOfDay);
        }

        [Fact]
        public void SetTime_Fail()
        {
            _viewModel.Format = CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.LongTimePattern;
            _viewModel.TimeInput = "1:02:30 PM";

            _viewModel.SetTime();

            Assert.Equal(default, _viewModel.Time);
        }

        private bool HasError()
        {
            return !string.IsNullOrWhiteSpace(_viewModel.Error);
        }
    }
}
