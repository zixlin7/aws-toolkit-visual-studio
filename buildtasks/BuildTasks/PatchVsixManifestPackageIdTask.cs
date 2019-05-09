using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BuildTasks
{
    /// <summary>
    /// Updates the PackageId element in the source.extension.vsixmanifest (constant name, per vsix rules)
    /// to append the guid of the package which is discovered by searching a source file for a named
    /// constant. 
    /// Patching the id allows us to debug our package when we have it installed from a release build, as 
    /// it gives the two packages distinct identities.
    /// </summary>
    public class PatchVsixManifestPackageIdTask : BuildTaskBase
    {
        private const string vsixManifestFilename = "source.extension.vsixmanifest";

        public string PackageRootFolder { get; set; }

        public string PackageIdPrefix { get; set; }

        public string GuidSourceFile { get; set; }

        public string GuidMemberName { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var vsixManifestFile = Path.Combine(PackageRootFolder, vsixManifestFilename);
            var guidSourceFile = Path.Combine(PackageRootFolder, GuidSourceFile);

            if (!Directory.Exists(PackageRootFolder))
            {
                Console.WriteLine("Directory {0} does not exist.", PackageRootFolder);
                return false;
            }

            if (!File.Exists(vsixManifestFile))
            {
                Console.WriteLine("Vsix manifest file {0} does not exist.", vsixManifestFile);
                return false;
            }

            if (!File.Exists(guidSourceFile))
            {
                Console.WriteLine("Guids source file {0} does not exist.", guidSourceFile);
                return false;
            }

            var packageGuid = FindPackageGuid(guidSourceFile, GuidMemberName);
            if (string.IsNullOrEmpty(packageGuid))
            {
                Console.WriteLine("Could not find package guid using member name {0} in guid source file {1}.", GuidMemberName, guidSourceFile);
                return false;
            }

            var doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(vsixManifestFile));

            PatchIdentifier(doc, string.Concat(PackageIdPrefix, "_", packageGuid));

            doc.Save(vsixManifestFile);

            return true;
        }

        static void PatchIdentifier(XmlDocument vsixManifest, string idContent)
        {
            // there should be exactly one, otherwise vsix manifest is not valid
            var identifierElement = vsixManifest.GetElementsByTagName("Identifier")[0] as XmlNode;
            var idAttribute = identifierElement.Attributes["Id"];
            idAttribute.Value = idContent;
        }

        static string FindPackageGuid(string sourceFile, string memberName)
        {
            // looking for 'memberName = "guid"', with some flexibility on spaces

            var source = File.ReadAllText(sourceFile);
            int memberStart = source.IndexOf(memberName, StringComparison.Ordinal);
            if (memberStart < 0)
                return string.Empty;

            int guidStart = memberStart + memberName.Length + 1;
            // didn't want to do string.split on file remainder, so parse by char just like the ol' C days
            while (source[guidStart] != '"')
                guidStart++;

            int guidEnd = ++guidStart;
            while (source[guidEnd] != '"')
                guidEnd++;

            return source.Substring(guidStart, guidEnd - guidStart);
        }
    }
}
