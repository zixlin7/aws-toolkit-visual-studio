using System;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ICloneCodeCommitRepositoryDialog
    {
        string LocalPath { get; }

        Uri RemoteUri { get; }

        string RepositoryName { get; }

        bool Show();
    }
}
