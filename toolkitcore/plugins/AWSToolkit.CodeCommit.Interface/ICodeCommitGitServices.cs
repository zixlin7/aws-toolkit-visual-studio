using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.CodeCommit.Interface
{
    public interface ICodeCommitGitServices
    {
        Task CloneAsync(ServiceSpecificCredentials credentials,
                                     string repositoryUrl,
                                     string localFolder);

        Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo,
                                      bool autoCloneNewRepository,
                                      AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback);
    }
}
