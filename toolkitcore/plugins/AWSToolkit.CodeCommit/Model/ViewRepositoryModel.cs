using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class ViewRepositoryModel
    {
        public ViewRepositoryModel(RepositoryWrapper repositoryWrapper)
        {
            RepositoryWrapper = repositoryWrapper;
        }

        public ViewRepositoryModel(RepositoryMetadata repositoryMetadata)
        {
            RepositoryWrapper = new RepositoryWrapper(repositoryMetadata);
        }

        public RepositoryWrapper RepositoryWrapper { get; }

        public string RepositoryName => RepositoryWrapper?.Name;
    }
}
