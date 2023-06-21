using System.Threading.Tasks;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.VisualStudio.Notification;
using FluentAssertions;
using Xunit;

namespace AWSToolkitPackage.Tests.Integration.Notification
{
    public class NotificationIntegrationTest
    {
        private readonly NotificationInfoBarManager _sut;
        private const string _sampleProductVersion = "1.39.0.0";
        private const string _manifestPath = S3FileFetcher.HOSTEDFILES_LOCATION + "Notifications/VisualStudio/VsInfoBar.json";

        public NotificationIntegrationTest()
        {
            _sut = new NotificationInfoBarManager(null, null, _sampleProductVersion);
        }

        /// <summary>
        /// This confirms that fetching was successful. Additional validations on
        /// Notification's members are achieved in the internal hosted files test repo
        /// </summary>
        [Fact]
        public async Task ShouldFetchNotificationAsync()
        {
            var notificationModel = await _sut.FetchNotificationsAsync(_manifestPath);

            notificationModel.Should().NotBeNull();
        }
    }
}
