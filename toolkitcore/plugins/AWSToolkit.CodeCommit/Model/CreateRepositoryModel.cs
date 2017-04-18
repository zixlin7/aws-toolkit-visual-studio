using System;
using System.IO;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CreateRepositoryModel : BaseRepositoryModel
    {
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged(nameof(Name)); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged(nameof(Description)); }
        }

        public string LocalFolder
        {
            get { return _localFolder; }
            set { _localFolder = value; NotifyPropertyChanged(nameof(LocalFolder)); }
        }

        public INewCodeCommitRepositoryInfo GetNewRepositoryInfo()
        {
            var info = new NewRepositoryInfo
            {
                OwnerAccount = Account,
                Region = SelectedRegion,
                Name = Name,
                Description = Description
            };

            var finalPathComponent = Path.GetFileName(LocalFolder);
            if (!finalPathComponent.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                info.LocalFolder = Path.Combine(LocalFolder, Name);
            }

            return info;
        }

        private string _name;
        private string _description;
        private string _localFolder;
    }
}
