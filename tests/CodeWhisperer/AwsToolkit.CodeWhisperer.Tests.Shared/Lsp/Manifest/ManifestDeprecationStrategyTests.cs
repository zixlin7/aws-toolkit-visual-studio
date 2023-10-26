using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AWSToolkit.Tests.Common.Context;

using AwsToolkit.VsSdk.Common.Settings;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    public class ManifestDeprecationStrategyTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly ManifestDeprecationStrategy _sut;

        private readonly ManifestSchema _schema =
            new ManifestSchema() { ManifestSchemaVersion = "1.2", IsManifestDeprecated = false };

        private static readonly int _toolkitCompatibleMajorVersion = 1;
        private static readonly string _manifestUrl = "abc/1/manifest.json";

        public ManifestDeprecationStrategyTests()
        {
            var options = new VersionManifestOptions()
            {
                MajorVersion = _toolkitCompatibleMajorVersion, CloudFrontUrl = _manifestUrl
            };
            _sut = new ManifestDeprecationStrategy(options, _settingsRepository, _toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public async Task CanShowNotificationAsync_WhenSchemaNotDeprecated()
        {
            _schema.IsManifestDeprecated = false;
            Assert.False(await _sut.CanShowNotificationAsync(_schema));
        }


        [Fact]
        public async Task CanShowNotificationAsync_WhenUserHasDismissedNotification()
        {
            _schema.IsManifestDeprecated = true;
            AddDismissedManifestDeprecation(CreateDismissedNotification(_toolkitCompatibleMajorVersion, _manifestUrl));

            Assert.False(await _sut.CanShowNotificationAsync(_schema));
        }

        public static readonly IEnumerable<object[]> NotificationNotDismissedData = new[]
        {
            new object[] { null },
            new object[] { CreateDismissedNotification(_toolkitCompatibleMajorVersion, "some-other-url") },
            new object[] { CreateDismissedNotification(_toolkitCompatibleMajorVersion + 1, _manifestUrl) },
        };

        [Theory]
        [MemberData(nameof(NotificationNotDismissedData))]
        public async Task CanShowNotificationAsync_WhenUserHasNotDismissedNotification(
            DismissedManifestDeprecation dismissedManifestDeprecation)
        {
            _schema.IsManifestDeprecated = true;
            AddDismissedManifestDeprecation(dismissedManifestDeprecation);

            Assert.True(await _sut.CanShowNotificationAsync(_schema));
        }


        [Fact]
        public async Task MarkNotificationAsDismissedAsync_WhenManifestNotSeenBefore()
        {
            Assert.Empty(GetDismissedManifestDeprecations());

            await _sut.MarkNotificationAsDismissedAsync();

            var actualManifestDeprecations = GetDismissedManifestDeprecations();
            Assert.Single(actualManifestDeprecations);
            var dismissedDeprecation = actualManifestDeprecations.First();

            Assert.Equal(_manifestUrl, dismissedDeprecation.ManifestUrl);
            Assert.Equal(_toolkitCompatibleMajorVersion, dismissedDeprecation.SchemaMajorVersion);
        }


        [Fact]
        public async Task MarkNotificationAsDismissedAsync_WhenManifestSeenBefore()
        {
            AddDismissedManifestDeprecation(CreateDismissedNotification(_toolkitCompatibleMajorVersion, _manifestUrl));

            Assert.Single(GetDismissedManifestDeprecations());
          
            await _sut.MarkNotificationAsDismissedAsync();


            var actualManifestDeprecations = GetDismissedManifestDeprecations();
            Assert.Single(actualManifestDeprecations);
        }

        private List<DismissedManifestDeprecation> GetDismissedManifestDeprecations()
        {
            return _settingsRepository.Settings.LspSettings.DismissedManifestDeprecations;
        }

        private void AddDismissedManifestDeprecation(DismissedManifestDeprecation deprecation)
        {
            var dismissedDeprecations = GetDismissedManifestDeprecations();
            dismissedDeprecations.Add(deprecation);
        }

        private static DismissedManifestDeprecation CreateDismissedNotification(int majorVersion, string url)
        {
            return new DismissedManifestDeprecation() { SchemaMajorVersion = majorVersion, ManifestUrl = url };
        }
    }
}
