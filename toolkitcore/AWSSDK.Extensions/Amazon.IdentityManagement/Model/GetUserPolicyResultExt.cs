using System.Web;

namespace Amazon.IdentityManagement.Model
{
    public static class GetUserPolicyResponseExt
    {
        public static string GetDecodedPolicyDocument(this GetUserPolicyResponse result)
        {
            return HttpUtility.UrlDecode(result.PolicyDocument);
        }
    }
}
