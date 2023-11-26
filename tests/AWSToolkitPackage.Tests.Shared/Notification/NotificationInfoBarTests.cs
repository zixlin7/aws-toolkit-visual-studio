#if VS2022_OR_LATER
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.Notification;

using AWSToolkitPackage.Tests.Utilities;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Notification
{
    [Collection(TestProjectMockCollection.CollectionName)]
    public class NotificationInfoBarTests
    {
        private readonly NotificationInfoBar _sut;
        private readonly Mock<IVsInfoBarUIElement> _element = new Mock<IVsInfoBarUIElement>();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<NotificationStrategy> _strategy;

        private static readonly List<IToolkitHostInfo> _unsupportedHosts = new List<IToolkitHostInfo>()
        {
            ToolkitHosts.AwsStudio,
            ToolkitHosts.Vs2008,
            ToolkitHosts.Vs2010,
            ToolkitHosts.Vs2012,
            ToolkitHosts.Vs2013,
            ToolkitHosts.Vs2015
        };

        public NotificationInfoBarTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();
            _strategy = new Mock<NotificationStrategy>(new FileSettingsRepository<NotificationSettings>(),
                _toolkitContextFixture.ToolkitContext, "1.39.0.0", Component.Infobar);
            _sut = new NotificationInfoBar(BuildNotification(), _strategy.Object, _toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public async Task DontShowAgainClicked()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var actionItem = new Mock<IVsInfoBarActionItem>();
            actionItem.Setup(mock => mock.ActionContext).Returns(new DontShowAgainAction());

            _sut.OnActionItemClicked(_element.Object, actionItem.Object);
            AssertInfoBarClosed();
        }

        [Theory]
        [MemberData(nameof(GetToolkitHostData))]
        public void ShouldShowMarketplacePage(IToolkitHostInfo host)
        {
            if (!_unsupportedHosts.Contains(host))
            {
                // Arrange
                _toolkitContextFixture.ToolkitContext.ToolkitHostInfo = host;
                var action = new ShowMarketplaceAction(_toolkitContextFixture.ToolkitContext, BuildNotification().NotificationId);

                // Act
                action.ShowMarketplace(_strategy.Object);

                // Assert
                _toolkitContextFixture.ToolkitHost.Verify(x => x.OpenInBrowser(It.IsAny<string>(), false), Times.Once);
            }
        }

        [Theory]
        [InlineData("es-ES")]
        [InlineData("es")]
        [UseCulture("es-ES")]
        public void ShouldGetLocalizedTextUsingCulture(string locale)
        {
            var notification = BuildLocalizedNotification(locale);

            var localizedText = NotificationInfoBar.GetLocalizedText(notification.Content,
                new List<string> { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.TwoLetterISOLanguageName });

            Assert.Equal($"test {locale} content", localizedText);
        }

        [Fact]
        [UseCulture("en-GB")]
        public void ShouldGetLocalizedTextUsingFallbackDefaultCulture()
        {
            var localizedText = NotificationInfoBar.GetLocalizedText(BuildNotification().Content,
                new List<string> { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.TwoLetterISOLanguageName });

            Assert.Equal("test en-US content", localizedText);
        }

        [Fact]
        [UseCulture("es-ES")]
        public void ShouldGetLocalizedDontShowAgainUsingCultureLanguage()
        {
            Assert.Equal("No mostrar de nuevo", _sut.GetLocalizedDontShowAgainDisplayText());
        }

        [Fact]
        [UseCulture("en-GB")]
        public void ShouldGetLocalizedDontShowAgainUsingFallbackDefaultCulture()
        {
            Assert.Equal("Don't Show Again", _sut.GetLocalizedDontShowAgainDisplayText());
        }

        public static TheoryData<IToolkitHostInfo> GetToolkitHostData()
        {
            var data = new TheoryData<IToolkitHostInfo>();

            typeof(ToolkitHosts).GetFields().ToList().ForEach(host => data.Add((IToolkitHostInfo) host.GetValue(null)));

            return data;
        }

        private void AssertInfoBarClosed()
        {
            _element.Verify(mock => mock.Close(), Times.Once);
        }

        private Amazon.AWSToolkit.VisualStudio.Notification.Notification BuildLocalizedNotification(string locale)
        {
            var notification = BuildNotification();
            notification.Content[locale] = $"test {locale} content";
            return notification;
        }

        private Amazon.AWSToolkit.VisualStudio.Notification.Notification BuildNotification()
        {
            return new Amazon.AWSToolkit.VisualStudio.Notification.Notification()
            {
                NotificationId = "uuid",
                Content = new Dictionary<string, string>
                {
                    { "en-US", "test en-US content" }
                },
                DisplayIf = new DisplayIf() { ToolkitVersion = "1.40.0.0", Comparison = "<" }
            };
        }
    }
}

#endif
