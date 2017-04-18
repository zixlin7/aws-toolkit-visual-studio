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
            set
            {
                _name = value;
                NotifyPropertyChanged(nameof(Name));

                SelectedFolder = Path.Combine(BaseFolder, _name);
            }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged(nameof(Description)); }
        }

        /// <summary>
        /// Contains the initial folder for cloning into, and any selections made
        /// by the user from the browse dialog. The selected repo name will be 
        /// appended to this to form the SelectedFolder value as the user chooses
        /// the repo/
        /// </summary>
        public string BaseFolder
        {
            get { return _baseFolder; }
            set
            {
                _baseFolder = value;
                SelectedFolder = !string.IsNullOrEmpty(Name) ? Path.Combine(_baseFolder, Name) : BaseFolder;
            }
        }

        public string SelectedFolder
        {
            get { return _selectedFolder; }
            set { _selectedFolder = value; NotifyPropertyChanged(nameof(SelectedFolder)); }
        }

        public INewCodeCommitRepositoryInfo GetNewRepositoryInfo()
        {
            var info = new NewRepositoryInfo
            {
                OwnerAccount = Account,
                Region = SelectedRegion,
                Name = Name,
                Description = Description,
                LocalFolder = SelectedFolder
            };

            return info;
        }

        private string _name;
        private string _description;
        private string _selectedFolder;
        private string _baseFolder;
    }
}
