using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AwsToolkit.VsSdk.Common.LambdaTester
{
    /// <summary>
    /// Lambda Tester representation of a Project file (eg: .csproj file)
    /// </summary>
    internal class Project
    {
        private readonly XDocument _document;

        public string FileContents { get; }

        internal Project(string filename)
        {
            FileContents = File.ReadAllText(filename);
            _document = XDocument.Parse(FileContents);
        }

        public string AwsProjectType => _document.XPathSelectElement("//PropertyGroup/AWSProjectType")?.Value;

        public string TargetFramework => _document.XPathSelectElement("//PropertyGroup/TargetFramework")?.Value;
    }
}
