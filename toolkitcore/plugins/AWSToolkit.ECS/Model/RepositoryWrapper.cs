using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;
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

        public bool IsLoaded => _repository != null;

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Repository";
            componentName = this._repository.RepositoryName;
        }

        [DisplayName("Name")]
        [AssociatedIcon(true, "RepositoryIcon")]
        public string Name => _repository != null ? _repository.RepositoryName : _repositoryArn.Split('/').LastOrDefault();

        [DisplayName("ARN")]
        public string RepositoryArn => _repositoryArn;

        [DisplayName("URI")]
        public string RepositoryUri => _repository.RepositoryUri;

        [DisplayName("Creation Date")]
        public string CreationDate => _repository.CreatedAt.ToString();

        [Browsable(false)]
        public string TypeName => "Repository";

        [Browsable(false)]
        public string DisplayName => string.Format("{0} ({1})", Name, _repositoryArn);

        [Browsable(false)]
        public System.Windows.Media.ImageSource RepositoryIcon
        {
            get
            {
                var icon = IconHelper.GetIcon("repository.png");
                return icon.Source;
            }
        }

        public string Region => _repositoryArn.Split(':')[3];

        private readonly RangeObservableCollection<ImageDetailWrapper> _images = new RangeObservableCollection<ImageDetailWrapper>();

        public ObservableCollection<ImageDetailWrapper> Images => _images;

        public void SetImages(ICollection<ImageDetailWrapper> images)
        {
            _images.Clear();
            _images.AddRange(images.OrderByDescending(x => x.NativeImageDetail.ImagePushedAt));
        }
    }
}
