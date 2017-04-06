using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class ViewRepositoryModel : INotifyPropertyChanged
    {
        public ViewRepositoryModel(RepositoryWrapper repositoryWrapper)
        {
            RepositoryWrapper = repositoryWrapper;
        }

        public ViewRepositoryModel(RepositoryMetadata repositoryMetadata)
        {
            RepositoryWrapper = new RepositoryWrapper(repositoryMetadata);
        }

        public ViewRepositoryModel()
        {
        }

        public RepositoryWrapper RepositoryWrapper { get; internal set; }

        public string Name => RepositoryWrapper?.Name;

        public string Description => RepositoryWrapper.Description;

        public string CloneUrlHttp => RepositoryWrapper.RepositoryUrl;

        public string LocalWorkspaceFolder
        {
            get { return RepositoryWrapper.LocalFolder; }
            set { RepositoryWrapper.LocalFolder = value; OnPropertyChanged(); }
        }

        public Uri LocalWorkspaceFolderUri
        {
            get
            {
                if (string.IsNullOrEmpty(LocalWorkspaceFolder))
                    return null;

                return new Uri(LocalWorkspaceFolder);
            }
        }

        public string CreationDate => RepositoryWrapper.RepositoryMetadata.CreationDate.ToShortDateString();

        public string LastModifiedDate => RepositoryWrapper.RepositoryMetadata.LastModifiedDate.ToShortDateString();

        public string DefaultBranch => RepositoryWrapper.RepositoryMetadata.DefaultBranch;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
