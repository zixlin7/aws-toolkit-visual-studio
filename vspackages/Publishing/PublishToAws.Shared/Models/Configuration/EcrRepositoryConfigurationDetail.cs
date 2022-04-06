using System.Diagnostics;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    [DebuggerDisplay("Ecr Repo: {Id} | {Value}")]
    public class EcrRepositoryConfigurationDetail : ConfigurationDetail
    {
        private ICommand _selectRepo;

        public ICommand SelectRepo
        {
            get => _selectRepo;
            set => SetProperty(ref _selectRepo, value);
        }
    }
}
