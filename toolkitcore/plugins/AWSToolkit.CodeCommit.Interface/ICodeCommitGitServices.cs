using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CodeCommit.Interface
{
    public interface ICodeCommitGitServices
    {
        Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo);
    }
}
