using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewRepositoryModel : BaseModel
    {
        private RepositoryWrapper _repositoryWrapper;

        public RepositoryWrapper Repository
        {
            get { return _repositoryWrapper; }
            internal set { _repositoryWrapper = value; NotifyPropertyChanged("Repository"); }
        }

    }
}
