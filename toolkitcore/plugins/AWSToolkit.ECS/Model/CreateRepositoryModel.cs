using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class CreateRepositoryModel : BaseModel
    {
        string _repositoryName;

        public CreateRepositoryModel()
        {
        }

        public string RepositoryName
        {
            get => this._repositoryName;
            set
            {
                this._repositoryName = value;
                this.NotifyPropertyChanged("RepositoryName");
            }
        }
    }
}
