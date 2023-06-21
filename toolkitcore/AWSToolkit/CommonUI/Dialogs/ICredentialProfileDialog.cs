using System;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ICredentialProfileDialog : IDisposable
    {
        bool Show();
    }
}
