using System;

using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ISsoLoginDialog : IDisposable
    {
        /// <summary>
        /// Represents credentials associated with an AWS IAM IDC Credential profile 
        /// </summary>
        ImmutableCredentials Credentials { get; set; }

        /// <summary>
        /// Represents SSO token associated with an AWS Builder ID connection
        /// </summary>
        SsoToken SsoToken { get; set; }

        /// <summary>
        /// Displays the dialog and performs and returns the result of the associated login process
        /// </summary>
        TaskResult DoLoginFlow();
    }
}
