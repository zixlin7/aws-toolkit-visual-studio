using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.ViewModels
{
    public abstract class BaseLogEntityViewModelTests<TViewModel> where TViewModel : BaseLogsViewModel
    {
        protected BaseLogsViewModelFixture ViewModelFixture;
        protected TViewModel ViewModel;

        protected string SampleToken => ViewModelFixture.SampleToken;
        protected List<LogGroup> SampleLogGroups => ViewModelFixture.SampleLogGroups;
        protected List<LogStream> SampleLogStreams => ViewModelFixture.SampleLogStreams;
        protected List<LogEvent> SampleLogEvents => ViewModelFixture.SampleLogEvents;
        protected Mock<ICloudWatchLogsRepository> Repository => ViewModelFixture.Repository;

        [Fact]
        public async Task LoadAsync_AdjustsLoadingLogs()
        {
            var loadingAdjustments = new List<bool>();

            ViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.LoadingLogs))
                {
                    loadingAdjustments.Add(ViewModel.LoadingLogs);
                }
            };

            Assert.False(ViewModel.LoadingLogs);

            await ViewModel.LoadAsync();

            Assert.Equal(2, loadingAdjustments.Count);
            Assert.True(loadingAdjustments[0]);
            Assert.False(loadingAdjustments[1]);
        }

        [Fact]
        public async Task LoadAsync_NoFilter_NoMetric()
        {
            ViewModel.FilterText = string.Empty;
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 0, 0);
        }

        [Fact]
        public async Task LoadAsync_UnchangedFilter_NoMetric()
        {
            ViewModel.FilterText = "some-filter";
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
        }

        [Fact]
        public async Task LoadAsync_RemoveFilter_NoMetric()
        {
            ViewModel.FilterText = "some-filter";
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
            ViewModel.FilterText = string.Empty;
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
        }

        [Fact]
        public async Task LoadAsync_AddFilter_EmitMetric()
        {
            ViewModel.FilterText = string.Empty;
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 0, 0);
            ViewModel.FilterText = "some-filter";
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
        }

        [Fact]
        public async Task LoadAsync_ChangeFilter_EmitMetric()
        {
            ViewModel.FilterText = "some-filter";
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 1, 0);
            ViewModel.FilterText = "some-filter-too";
            await ViewModel.LoadAsync();
            ViewModelFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsFilter(
                ViewModel.GetCloudWatchResourceType(), 2, 0);
        }
    }
}
