using System.Web;

namespace Amazon.IdentityManagement.Model
{
    public static class GetGroupPolicyResponseExt
    {
        public static string GetDecodedPolicyDocument(this GetGroupPolicyResponse result)
        {
            return HttpUtility.UrlDecode(result.PolicyDocument);
        }
    }
}
