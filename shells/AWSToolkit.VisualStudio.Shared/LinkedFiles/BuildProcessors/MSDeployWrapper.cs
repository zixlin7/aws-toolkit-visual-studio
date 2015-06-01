using System;
using System.Text;
using System.Diagnostics;
using System.IO;

using Microsoft.Win32;

using Amazon.AWSToolkit.VisualStudio.Shared.Loggers;

namespace Amazon.AWSToolkit.VisualStudio.Shared.BuildProcessors
{
    internal class MSDeployWrapper
    {
        #region constants
        // suffix with project & version to make it easy to find on error cases
        const string ARCHIVE_NAME_PATTERN = "AWSDeploymentArchive_{0}_{1}.zip";

        const string MSDEPLOY_EXE = "msdeploy.exe";
        const string MSDEPLOY_PACKAGE_PROVIDER = "package";
        const string MSDEPLOY_ARCHIVE_PROVIDER = "archivedir";
        const string MSDEPLOY_COMMANDARGS_PATTERN = "-verb:sync -source:manifest=\"{0}\" -dest:{1}=\"{2}\" {3}";

        // could use xml api to create file for us but until the file(s) contain
        // complex content, it's just as easy to output raw text
        const string XML_FILE_HEADER = "<?xml version=\"1.0\"?>";
        const string SITEMANIFEST_START_TAG = "<sitemanifest>";
        // note - don't pass enable32BitAppOnWin64 switch here, we'll fail to deploy on host (we detect this and
        // switch later)
        const string SITEMANIFEST_IIS_ELEMENT_PATTERN = "<IisApp path=\"{0}\" managedRuntimeVersion=\"v{1}\" />";
        const string SITEMANIFEST_SETACL_DIR_ELEMENT_PATTERN = "<setAcl path=\"{0}\" setAclResourceType=\"Directory\" />";
        const string SITEMANIFEST_SETACL_DIR_USER_ELEMENT_PATTERN = "<setAcl path=\"{0}\" setAclUser=\"anonymousAuthenticationUser\" setAclResourceType=\"Directory\" />";
        const string SITEMANIFEST_END_TAG = "</sitemanifest>";

        // declareParam option and supporting constants
        const string DECLPARAM_PATTERN = "-declareParam:name=\"{0}\",kind=\"{1}\",scope=\"{2}\",match=\"{3}\"";
        const string DECLPARAM_DEFAULT_VALUE_ATTR_PATTERN = ",defaultValue=\"{0}\"";
        const string DECLPARAM_TAGS_ATTR_PATTERN = ",tags={0}";

        const string DECLPARAM_PARAMNAME_IISAPPNAME = "IIS Web Application Name";
        const string DECLPARAM_KIND_PROVIDERPATH = "ProviderPath";
        const string DECLPARAM_SCOPE_IISAPP = "IisApp";
        const string DECLPARAM_SCOPE_SETACL = "setAcl";
        const string DECLPARAM_TAG_IISAPP = "IisApp";

        const string MSDEPLOY_PROBE_REGISTRY_PATH = @"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\MSDeploy";
        #endregion

        StringBuilder _declaredParams;
        string _manifestFilename;
        readonly string _projectName;
        readonly string _contentLocation;
        readonly string _targetRuntime;
        readonly bool _isIncremental;
        readonly string _iisAppPath;

        public MSDeployWrapper(string projectName, string contentLocation, string targetRuntime, string iisAppPath, bool isIncremental)
        {
            this._projectName = projectName;
            this._contentLocation = contentLocation;
            this._targetRuntime = targetRuntime;
            this._iisAppPath = iisAppPath;
            this._isIncremental = isIncremental;

            GenerateManifest();
            GenerateParams();
        }

        /// <summary>
        /// Gets a file naming pattern to be used for the generated pattern
        /// </summary>
        public static string ArchiveNamePattern { get { return ARCHIVE_NAME_PATTERN; } }

        /// <summary>
        /// Returns the name of the generated manifest file
        /// </summary>
        public string ManifestFilename { get { return _manifestFilename; } }

        /// <summary>
        /// Generates deployment manifest for staged content
        /// </summary>
        /// <param name="outputPackageFilename">Where the website/webapp content was staged after build</param>
        /// <param name="logger"></param>
        public void Run(string outputPackageFilename, IBuildAndDeploymentLogger logger)
        {
            string manifestCmd = string.Format(MSDEPLOY_COMMANDARGS_PATTERN,
                                               ManifestFilename,
                                               _isIncremental ? MSDEPLOY_ARCHIVE_PROVIDER : MSDEPLOY_PACKAGE_PROVIDER,
                                               outputPackageFilename,
                                               _declaredParams != null ? _declaredParams.ToString() : string.Empty);

            var psi = new ProcessStartInfo
            {
                FileName = MsDeployExe,
                Arguments = manifestCmd,

                // shouldn't be needed as we're fully qualifying in command line, just in case...
                WorkingDirectory = this._contentLocation,
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            logger.OutputMessage(string.Format("...starting {0} {1}", psi.FileName, psi.Arguments));

            using (var proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                if (psi.RedirectStandardOutput)
                {
                    while (true)
                    {
                        try
                        {
                            string output;
                            if ((output = proc.StandardOutput.ReadLine()) != null)
                                logger.OutputMessage("......output: " + output);
                            string error;
                            if ((error = proc.StandardError.ReadLine()) != null)
                                logger.OutputMessage("......error: " + error);
                            if (output == null && error == null)
                                break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                    proc.WaitForExit();

            }
        }

        void GenerateManifest()
        {
            _manifestFilename = Path.GetTempFileName();
            using (TextWriter writer = new StreamWriter(_manifestFilename, false, Encoding.UTF8))
            {
                writer.WriteLine(XML_FILE_HEADER);
                writer.WriteLine(SITEMANIFEST_START_TAG);
                writer.WriteLine(SITEMANIFEST_IIS_ELEMENT_PATTERN, _contentLocation, _targetRuntime);
                writer.WriteLine(SITEMANIFEST_SETACL_DIR_ELEMENT_PATTERN, _contentLocation);
                writer.WriteLine(SITEMANIFEST_SETACL_DIR_USER_ELEMENT_PATTERN, _contentLocation);
                writer.WriteLine(SITEMANIFEST_END_TAG);
            }
        }

        void GenerateParams()
        {
            _declaredParams = new StringBuilder();

            string escapedContentLocation = EscapeAllCharacters(this._contentLocation);
            _declaredParams.AppendFormat(DECLPARAM_PATTERN,
                                         DECLPARAM_PARAMNAME_IISAPPNAME,
                                         DECLPARAM_KIND_PROVIDERPATH,
                                         DECLPARAM_SCOPE_IISAPP,
                                         string.Format("{0}{1}", "^", escapedContentLocation));

            _declaredParams.AppendFormat(DECLPARAM_DEFAULT_VALUE_ATTR_PATTERN,
                                         string.IsNullOrEmpty(_iisAppPath)
                                             ? string.Format("{0}/{1}", "Default Web Site", this._projectName)
                                             : _iisAppPath);

            _declaredParams.AppendFormat(DECLPARAM_TAGS_ATTR_PATTERN,
                                         DECLPARAM_TAG_IISAPP);

            _declaredParams.Append(' ');
            _declaredParams.AppendFormat(DECLPARAM_PATTERN,
                                         DECLPARAM_PARAMNAME_IISAPPNAME,
                                         DECLPARAM_KIND_PROVIDERPATH,
                                         DECLPARAM_SCOPE_SETACL,
                                         string.Format("{0}{1}", "^", escapedContentLocation));
            _declaredParams.AppendFormat(DECLPARAM_TAGS_ATTR_PATTERN,
                                         DECLPARAM_TAG_IISAPP);
        }

        /// <summary>
        /// Probes registry for install location of vs msdeploy.exe; if not found then
        /// fallback to just exename
        /// </summary>
        string MsDeployExe
        {
            get
            {
                var msdeployPath = string.Empty;
                using (var msdeployKey = Registry.LocalMachine.OpenSubKey(MSDEPLOY_PROBE_REGISTRY_PATH, false))
                {
                    msdeployPath = msdeployKey.GetValue(null) as string;
                }

                if (!string.IsNullOrEmpty(msdeployPath))
                    return Path.Combine(msdeployPath, MSDEPLOY_EXE);
                else
                    return MSDEPLOY_EXE;
            }
        }

        // strings provided to msdeploy are usually regex's, so we must escape everything
        static string EscapeAllCharacters(string source)
        {
            string temp = source;
            temp = temp.Replace(@"\", @"\\");
            temp = temp.Replace(" ", @"\ ");
            temp = temp.Replace("(", @"\(");
            temp = temp.Replace(")", @"\)");
            temp = temp.Replace(".", @"\.");
            return temp;
        }

        private MSDeployWrapper() { }
    }
}