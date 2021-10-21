using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

using Amazon.S3.Model;

namespace Amazon.AWSToolkit.Solutions
{
    public class ProjectFile
    {
        public string TargetFramework { get; }
        public ProjectFile(string filepath)
        {
            var fileContents = File.ReadAllText(filepath);
            var _document = XDocument.Parse(fileContents);

            TargetFramework = _document.XPathSelectElement("//PropertyGroup/TargetFramework")?.Value;
        }
    }
}
