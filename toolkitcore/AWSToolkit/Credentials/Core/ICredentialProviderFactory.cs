using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Factory for adding new credential to the central credential management system
    /// </summary>
    public interface ICredentialProviderFactory: IDisposable
    {
        // ID used to uniquely identify this factory
        string Id { get; }

        /// <summary>
        ///  Invoked on creation of the factory to update the credential system with what <see cref="ICredentialIdentifier"/> this factory
        /// is capable of creating.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Creates a <see cref="ToolkitCredentials"/> for the specified CredentialIdentifier
        /// (<see cref="ICredentialIdentifier"/>) and region (<see cref="ToolkitRegion"/>)
        /// </summary>
        ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region);

        /// <summary>
        /// Checks if a user login prompt is required for the given <see cref="ICredentialIdentifier"/>.
        /// </summary>
        /// <param name="id"></param>
        bool IsLoginRequired(ICredentialIdentifier id);

        /// <summary>
        /// Returns the ICredentialProviderFactory if it implements the <see cref="ICredentialProfileProcessor"/>, else null
        /// </summary>
        /// <returns></returns>
        ICredentialProfileProcessor GetCredentialProfileProcessor();

        /// <summary>
        /// Returns the list of <see cref="ICredentialIdentifier"/> found on initial load of the factory
        /// </summary>
        /// <returns></returns>
        List<ICredentialIdentifier> GetCredentialIdentifiers();

        event EventHandler<CredentialChangeEventArgs> CredentialsChanged;
    }
}
