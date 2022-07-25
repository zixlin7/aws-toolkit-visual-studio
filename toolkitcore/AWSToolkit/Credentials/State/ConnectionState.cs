using System;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Credentials.State
{

    /// <summary>
    /// A state machine around the connection validation steps the toolkit goes through.
    /// It attempts to encapsulate both state and data available at each state and
    /// a consistent place to determine how to display state information ([Message]).
    /// It also exposes an [isTerminal] property that indicates if this
    /// state is temporary in the 'connection validation' workflow or if this is a terminal state.
    /// </summary>
    public class ConnectionState
    {
        public static bool IsValid(ConnectionState connectionState)
        {
            return connectionState is ValidConnection;
        }

        public string Message { get; set; }
        public bool IsTerminal { get; set; }

        protected ConnectionState(string message, bool isTerminal)
        {
            Message = message;
            IsTerminal = isTerminal;
        }

        public class InitializingToolkit : ConnectionState
        {
            public InitializingToolkit() : base("Toolkit initializing", false)
            {
            }
        }

        public class ValidatingConnection : ConnectionState
        {
            public ValidatingConnection() : base("Validating connection", false)
            {
            }
        }

        public class InvalidConnection : ConnectionState
        {
            public InvalidConnection(string message) : base($"Unable to connect to AWS: {Environment.NewLine}{message}",
                true)
            {
            }
        }

        public class ValidConnection : ConnectionState
        {
            public ValidConnection(ICredentialIdentifier identifier, ToolkitRegion region) : base(
                $"{identifier.DisplayName} loaded for region: {region.DisplayName}", true)
            {
            }
        }

        public class UserAction : ConnectionState
        {
            //pops a user prompt
            public UserAction(string userActionMessage) : base(userActionMessage, true)
            {
            }
        }

        public class IncompleteConfiguration : ConnectionState
        {
            public IncompleteConfiguration(ICredentialIdentifier identifier, ToolkitRegion region) : base(
                ModifyMessage(identifier, region), false)
            {
            }

            private static string ModifyMessage(ICredentialIdentifier identifier, ToolkitRegion region)
            {
                if (identifier == null && region == null)
                {
                    return "No region or credential selected";
                }

                if (region == null)
                {
                    return "No region selected";
                }

                if (identifier == null)
                {
                    return "No credentials selected";
                }

                throw new ArgumentException(
                    $"At least one of regionId {region} or credential identifier {identifier} must be null");
            }
        }
    }
}
