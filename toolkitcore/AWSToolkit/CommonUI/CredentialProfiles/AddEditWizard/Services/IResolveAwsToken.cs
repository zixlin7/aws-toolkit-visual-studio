using System.Threading.Tasks;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public interface IResolveAwsToken
    {
        Task<AWSToken> ResolveAwsTokenAsync();
    }
}
