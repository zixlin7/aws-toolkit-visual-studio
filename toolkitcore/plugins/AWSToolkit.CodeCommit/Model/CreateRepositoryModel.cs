using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CreateRepositoryModel : BaseRepositoryModel
    {
        private string _name;
        private string _description;
        private string _selectedFolder;
        private string _baseFolder;
        private GitIgnoreOption _selectedGitIgnore;

        private readonly List<GitIgnoreOption> _gitIgnoreOptions = new List<GitIgnoreOption>
        {
            new GitIgnoreOption { DisplayText = "Visual Studio file types", GitIgnoreType = GitIgnoreOption.OptionType.VSToolkitDefault },
            new GitIgnoreOption { DisplayText = "Use custom...", GitIgnoreType = GitIgnoreOption.OptionType.Custom },
            new GitIgnoreOption { DisplayText = "No .gitignore file", GitIgnoreType = GitIgnoreOption.OptionType.None }
        };

        public CreateRepositoryModel()
        {
            SelectedGitIgnore = _gitIgnoreOptions.FirstOrDefault();
        }

        public string Name
        {
            get => _name;
            set
            {
                SetProperty(ref _name, value);
                SelectedFolder = Path.Combine(BaseFolder, _name);
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Contains the initial folder for cloning into, and any selections made
        /// by the user from the browse dialog. The selected repo name will be 
        /// appended to this to form the SelectedFolder value as the user chooses
        /// the repo/
        /// </summary>
        public string BaseFolder
        {
            get => _baseFolder;
            set
            {
                _baseFolder = value;
                SelectedFolder = !string.IsNullOrEmpty(Name) ? Path.Combine(_baseFolder, Name) : BaseFolder;
            }
        }

        public string SelectedFolder
        {
            get => _selectedFolder;
            set => SetProperty(ref _selectedFolder, value);
        }

        public List<GitIgnoreOption> GitIgnoreOptions => _gitIgnoreOptions;

        public GitIgnoreOption SelectedGitIgnore
        {
            get => _selectedGitIgnore;
            set => SetProperty(ref _selectedGitIgnore, value);
        }

        public INewCodeCommitRepositoryInfo GetNewRepositoryInfo()
        {
            return new NewRepositoryInfo
            {
                OwnerAccount = Account,
                Region = SelectedRegion,
                Name = Name,
                Description = Description,
                LocalFolder = SelectedFolder,
                GitIgnore = SelectedGitIgnore
            };
        }
    }
}
