using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.ProjectJson
{
    public class NETCoreServerlessWithTestsWizardImplementation : NETCoreServerlessWizardImplementation
    {

        protected override void ApplyBlueprint(Dictionary<string, string> replacementsDictionary, ZipFile blueprintZipFile)
        {
            ZipEntry srcEntry = blueprintZipFile.GetEntry("src.zip");
            if (srcEntry != null)
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, blueprintZipFile.GetInputStream(srcEntry), replacementsDictionary["$safeprojectname$"]);
            }

            ZipEntry testEntry = blueprintZipFile.GetEntry("test.zip");
            if (testEntry != null)
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, blueprintZipFile.GetInputStream(testEntry), replacementsDictionary["$safeprojectname$"] + ".Tests");
            }
        }
    }
}
