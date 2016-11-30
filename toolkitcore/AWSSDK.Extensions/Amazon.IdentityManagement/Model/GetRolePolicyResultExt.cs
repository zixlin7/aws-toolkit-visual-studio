using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Amazon.IdentityManagement.Model
{
    public static class GetRolePolicyResponseExt
    {
        public static string GetDecodedPolicyDocument(this GetRolePolicyResponse result)
        {
            return HttpUtility.UrlDecode(result.PolicyDocument);
        }
    }
}
