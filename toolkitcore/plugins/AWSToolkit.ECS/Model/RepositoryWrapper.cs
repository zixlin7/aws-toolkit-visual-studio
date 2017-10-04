using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;

using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class RepositoryWrapper : PropertiesModel, IWrapper
    {
        private Repository _repository;
        private readonly string _repositoryArn;

        public RepositoryWrapper(Repository repository)
        {
            _repository = repository;
            _repositoryArn = repository.RepositoryArn;
        }

        public RepositoryWrapper(string arn)
        {
            _repositoryArn = arn;
        }

        public void LoadFrom(Repository repository)
        {
            _repository = repository;
        }

        public bool IsLoaded
        {
            get { return _repository != null; }
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Repository";
            componentName = this._repository.RepositoryName;
        }

        [DisplayName("Name")]
        [AssociatedIcon(true, "RepositoryIcon")]
        public string Name
        {
            get
            {
                return _repository != null ? _repository.RepositoryName : _repositoryArn.Split('/').LastOrDefault();
            }
        }

        [DisplayName("ARN")]
        public string RepositoryArn
        {
            get { return _repositoryArn; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Repository"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                return string.Format("{0} ({1})", Name, _repositoryArn);
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource RepositoryIcon
        {
            get
            {
                var icon = IconHelper.GetIcon("repository.png");
                return icon.Source;
            }
        }
    }
}
