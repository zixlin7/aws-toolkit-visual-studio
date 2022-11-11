using System;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ICloneCodeCatalystRepositoryDialog
    {
        AwsConnectionSettings ConnectionSettings { get; }

        string RepositoryName { get; }

        Uri CloneUrl { get; }

        string LocalPath { get; }

        bool Show();
    }
}
