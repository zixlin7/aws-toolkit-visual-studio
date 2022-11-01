using System;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ICloneCodeCatalystRepositoryDialog
    {
        string RepositoryName { get; }

        Uri CloneUrl { get; }

        string LocalPath { get; }

        bool Show();
    }
}
