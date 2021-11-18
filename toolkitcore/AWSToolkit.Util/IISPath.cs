using System.Linq;

namespace Amazon.AWSToolkit
{
    public class IisPath
    {
        public string WebSite { get; }
        public string AppPath { get; }

        public IisPath(string iisPath)
        {
            iisPath = iisPath.Trim();

            if (iisPath.Contains("/"))
            {
                var positionOfFirstSlash = iisPath.IndexOf("/");
                WebSite = iisPath.Substring(0, positionOfFirstSlash);
                AppPath = TrimTrailingSlash(iisPath.Substring(positionOfFirstSlash));
            }
            else
            {
                WebSite = "Default Web Site";
                AppPath = "/" + iisPath;
            }
        }

        private string TrimTrailingSlash(string s) => HasTrailingSlash(s) ? RemoveLastCharacter(s) : s;

        private bool HasTrailingSlash(string s) => s.EndsWith("/") && s.Count(f => f == '/') > 1;

        private string RemoveLastCharacter(string s) => s.Substring(0, s.Length - 1);
    }
}
