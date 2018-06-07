using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewRepositoryModel : BaseModel
    {
        private RepositoryWrapper _repositoryWrapper;

        public RepositoryWrapper Repository
        {
            get { return _repositoryWrapper; }
            internal set
            {
                _repositoryWrapper = value;
                NotifyPropertyChanged("Repository");
            }
        }

        #region Get Login Commands
        public string PowerShellGetLoginCommand
        {
            get { return string.Format("$loginCommand = Get-ECRLoginCommand -Region {0}", _repositoryWrapper.Region); }
        }

        public string AwsCliGetLoginCommand
        {
            get { return string.Format("aws ecr get-login --no-include-email --region {0}", _repositoryWrapper.Region); }
        }
        #endregion

        #region Run Login Commands
        public string PowerShellRunLoginCommand
        {
            get { return "Invoke-Expression $loginCommand.Command"; }
        }

        public string AwsCliRunLoginCommand
        {
            get { return string.Format("Invoke-Expression -Command (aws ecr get-login --no-include-email --region {0})", _repositoryWrapper.Region); }
        }
        #endregion

        #region Image Build Command

        public string DockerBuildCommand
        {
            get { return string.Format("docker build -t {0} .", _repositoryWrapper.Name); }
        }
        #endregion

        #region Image Tag Command

        public string DockerTagCommand
        {
            get { return string.Format("docker tag {0}:latest {1}:latest", _repositoryWrapper.Name, _repositoryWrapper.RepositoryUri); }
        }
        #endregion

        #region Docker Push Command

        public string DockerPushCommand
        {
            get { return string.Format("docker push {0}:latest", _repositoryWrapper.RepositoryUri); }
        }
        #endregion

    }
}
