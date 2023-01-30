﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Moq;
using Amazon.Runtime;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    /// <summary>
    /// Convenience class to create a starter set of ToolkitContext Mocks
    /// </summary>
    public class ToolkitContextFixture
    {
        public ToolkitContext ToolkitContext { get; }

        public TelemetryFixture TelemetryFixture { get; } = new TelemetryFixture();

        public Mock<ICredentialManager> CredentialManager { get; } = new Mock<ICredentialManager>();
        public Mock<ICredentialSettingsManager> CredentialSettingsManager { get; } = new Mock<ICredentialSettingsManager>();
        public Mock<IRegionProvider> RegionProvider { get; } = new Mock<IRegionProvider>();
        public Mock<IAwsServiceClientManager> ServiceClientManager { get; } = new Mock<IAwsServiceClientManager>();
        public Mock<ITelemetryLogger> TelemetryLogger => TelemetryFixture.TelemetryLogger;
        public Mock<IAWSToolkitShellProvider> ToolkitHost { get; } = new Mock<IAWSToolkitShellProvider>();
        public Mock<IDialogFactory> DialogFactory { get; } = new Mock<IDialogFactory>();

        public ToolkitContextFixture()
        {
            ToolkitHost.Setup(mock => mock.GetDialogFactory()).Returns(DialogFactory.Object);

            ToolkitContext = new ToolkitContext()
            {
                CredentialManager = CredentialManager.Object,
                CredentialSettingsManager = CredentialSettingsManager.Object,
                RegionProvider = RegionProvider.Object,
                ServiceClientManager = ServiceClientManager.Object,
                TelemetryLogger = TelemetryLogger.Object,
                ToolkitHost = ToolkitHost.Object
            };

            InitializeRegionProviderMocks();
        }

        private void InitializeRegionProviderMocks()
        {
            RegionProvider.Setup(mock => mock.IsRegionLocal(It.IsAny<string>())).Returns(false);
        }

        public void DefineCredentialProperties(ICredentialIdentifier credentialIdentifier, ProfileProperties profileProperties)
        {
            CredentialSettingsManager.Setup(m => m.GetProfileProperties(credentialIdentifier))
                .Returns(profileProperties);
        }

        public void SetupExecuteOnUIThread()
        {
            ToolkitHost.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());
        }

        public void SetupRegionAsLocal(string regionId)
        {
            RegionProvider.Setup(mock => mock.IsRegionLocal(regionId)).Returns(true);
        }

        public void DefineRegion(ToolkitRegion region)
        {
            RegionProvider.Setup(mock => mock.GetRegion(region.Id)).Returns(region);
        }

        public void SetupCreateServiceClient<TServiceClient>(TServiceClient client) where TServiceClient : class
        {
            ServiceClientManager.Setup(mock => mock.CreateServiceClient<TServiceClient>(
                It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns(client);

            ServiceClientManager.Setup(mock => mock.CreateServiceClient<TServiceClient>(
                It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>(), It.IsAny<ClientConfig>()))
                .Returns(client);
        }

        public void SetupGetToolkitCredentials(ToolkitCredentials credentials)
        {
            CredentialManager.Setup(
                mock => mock.GetToolkitCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialId, region) => credentials);
        }

        public void DefineCredentialIdentifiers(IEnumerable<ICredentialIdentifier> credentialIdentifiers)
        {
            CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(credentialIdentifiers.ToList());
        }

        public void SetupCredentialManagerSupports(ICredentialIdentifier credentialIdentifier,
            AwsConnectionType connectionType, bool isSupported)
        {
            CredentialManager.Setup(mock => mock.Supports(credentialIdentifier, connectionType)).Returns(isSupported);
        }

        public void SetupConfirm(bool returnValue)
        {
            ToolkitHost.Setup(
                    mock => mock.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>()))
                .Returns(returnValue);
        }
    }
}
