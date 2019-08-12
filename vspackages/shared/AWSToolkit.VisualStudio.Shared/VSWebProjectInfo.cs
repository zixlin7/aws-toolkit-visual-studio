using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Build.BuildEngine;

using System.ComponentModel;
using Microsoft.Build.Evaluation;

// helps distinguish the two 'Project' options we can be dealing with
using MSBuildProject = Microsoft.Build.Evaluation.Project;

using EnvDTEProject = EnvDTE.Project;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Shared
{
    /// <summary>
    /// Wrapper around VS web site and web app projects carrying info
    /// required to successfully build and deploy both project types using just
    /// the settings a dev would employ on a MS deployment.
    /// </summary>
    /// <remarks>
    /// Web application project types (that have a project file) have all of their
    /// settings held in msbuild project objects. Web site projects (no project file,
    /// settings in solution file) have their settings split between the DTE.Solution.Properties
    /// collection and a build-time 'wrapper' ms build project that holds the remaining
    /// build-config dependant properties.
    /// </remarks>
    public class VSWebProjectInfo
    {
        string _cachedProjectName;
        string _cachedProjectPathAndName;
        MSBuildProject _buildProject; // real or synthesized from solution file, delay loaded
        static readonly ILog Logger = LogManager.GetLogger(typeof(VSWebProjectInfo));

        const string TargetFrameworkVersionProperty = "TargetFrameworkVersion"; // used in web app projects
        const string TargetFrameworkMonikerProperty = "TargetFrameworkMoniker"; // used in web site projects
        const string TargetFrameworkProperty = "TargetFramework"; // also used in both, returns framework code

        public const string RuntimeV2_0 = "2.0";
        public const string RuntimeV4_0 = "4.0";

        /// <summary>
        /// 'well known' codes indicating target framework versions
        /// </summary>
        enum TargetFrameworkCode
        {
            Fx45 = 262149, // as of RC
            Fx40 = 262144,
            Fx35 = 196613,
            Fx30 = 196608,
            Fx20 = 131072
        }

        // guids identifying different types of web project that VS supports
        private const string guidWebApplicationProject = "{349C5851-65DF-11DA-9384-00065B846F21}"; // has project file
        private const string guidWebSiteProject = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}"; // no project file, settings in sln file

        // this makes our handling work more seamlessly across traditional web projects
        // and coreclr web projects that currently have no type guid. 
        public const string guidAWSPrivateCoreCLRWebProject = "{673502E5-F177-4CAA-B653-7C730A251794}";

        static readonly Dictionary<string, VsWebProjectType> VsWebProjectKinds 
            = new Dictionary<string, VsWebProjectType>(StringComparer.InvariantCultureIgnoreCase);

        public enum VsWebProjectType
        {
            NotWebProjectType,
            WebApplicationProject,
            WebSiteProject,
            CoreCLRWebProject
        }

        static VSWebProjectInfo()
        {
            VsWebProjectKinds.Add(guidWebApplicationProject, VsWebProjectType.WebApplicationProject);
            VsWebProjectKinds.Add(guidWebSiteProject, VsWebProjectType.WebSiteProject);
        }

        public VSWebProjectInfo(IVsHierarchy projHier, string projectIDGuid, string projectTypeGuid)
        {
            VsHierarchy = projHier;
            ProjectIDGuid = projectIDGuid;
            SetWebProjectType(projectTypeGuid);
        }

        /// <summary>
        /// Returns the evaluated project or solution build object
        /// </summary>
        public MSBuildProject BuildProject => _buildProject ?? (_buildProject = LoadMSBuildEvaluation());

        public static bool IsWebProjectType(string projectTypeGuid)
        {
            return VsWebProjectKinds.ContainsKey(projectTypeGuid);
        }

        /// <summary>
        /// Returns an indication of the type of web project we have
        /// </summary>
        public VsWebProjectType VsProjectType { get; protected set; }

        /// <summary>
        /// The internal VS hierarchy to the project instance
        /// </summary>
        public IVsHierarchy VsHierarchy { get; protected set; }

        /// <summary>
        /// The unique id of the project (in Guid format 'B', {xxxx....yyy})
        /// </summary>
        public string ProjectIDGuid { get; protected set; }

        /// <summary>
        /// Returns the full path and filename of the project; for web site
        /// projects this is just a path.
        /// </summary>
        public string VsProjectLocationAndName => CachedProjectPathAndName;

        /// <summary>
        /// Rooted path (not including filename) of the web site or web 
        /// application project
        /// </summary>
        public string VsProjectLocation => Path.GetDirectoryName(CachedProjectPathAndName);

        /// <summary>
        /// Name of the project file less path; empty string for web site projects
        /// that have no project file
        /// </summary>
        public string VsProjectFilename 
        { 
            get
            {
                if (VsProjectType == VsWebProjectType.WebApplicationProject)
                    return Path.GetFileName(CachedProjectPathAndName);

                return string.Empty;
            }
        }

        /// <summary>
        /// Logical name of the project; for web application projects
        /// this is the extension-less project filename. For web site
        /// projects, this is the parent folder name. 
        /// </summary>
        public string ProjectName 
        { 
            get
            {
                if (string.IsNullOrEmpty(_cachedProjectName))
                {
                    object value;
                    if (VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out value)
                                == VSConstants.S_OK && value != null)
                        _cachedProjectName = value as string;
                }

                return _cachedProjectName;
            }
        }

        /// <summary>
        /// Returns the runtime version selected for the project in its build properties.
        /// For web application projects, this data is inside the project file. For web site projects, 
        /// it's held in the WebsiteProperties ProjectSection for the project in the solution file.
        /// </summary>
        public string TargetRuntime
        {
            get
            {
                try
                {
                    switch (VsProjectType)
                    {
                        case VsWebProjectType.NotWebProjectType:
                            return string.Empty;

                        case VsWebProjectType.WebApplicationProject:
                            {
                                // should come back as 'vX.Y'
                                var framework = BuildProject.QueryPropertyValue(TargetFrameworkVersionProperty);
                                if (!string.IsNullOrEmpty(framework))
                                {
                                    if (framework.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
                                        framework = framework.Substring(1);

                                    if (framework.StartsWith("2", StringComparison.Ordinal) || framework.StartsWith("3", StringComparison.Ordinal))
                                        return RuntimeV2_0;

                                    return RuntimeV4_0;
                                }

                                return string.Empty;
                            }

                        case VsWebProjectType.WebSiteProject:
                            {
                                var prop = DTEProject.Properties.Item(TargetFrameworkProperty);
                                try
                                {
                                    var vrnCode = prop.Value.ToString();
                                    var vrnCodeVal = Int32.Parse(vrnCode); // 'Value as string' does not work
                                    if (Enum.IsDefined(typeof(TargetFrameworkCode), vrnCodeVal))
                                    {
                                        var fx = (TargetFrameworkCode)(Enum.Parse(typeof(TargetFrameworkCode), vrnCode));
                                        switch (fx)
                                        {
                                            case TargetFrameworkCode.Fx35:
                                            case TargetFrameworkCode.Fx30:
                                            case TargetFrameworkCode.Fx20:
                                                return RuntimeV2_0;
                                            default:
                                                return RuntimeV4_0;
                                        }
                                    }
                                }
                                catch (NullReferenceException) { }

                                // try and use parseable moniker format before giving up
                                prop = DTEProject.Properties.Item(TargetFrameworkMonikerProperty);
                                try
                                {
                                    // '.NETFramework,Version=vX.Y'
                                    var moniker = prop.Value as string;
                                    var parts = moniker.Split('=');
                                    return parts[1].Substring(1);
                                }
                                catch (NullReferenceException) { }
                            }
                            break;
                    }
                }
                catch (Exception e) 
                {
                    Logger.ErrorFormat("Failed to obtain target framework version for project {0}, exception {1}",
                                        CachedProjectPathAndName,
                                        e.Message);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the set of type guids associated with the specified project
        /// </summary>
        /// <param name="projectHierarchy"></param>
        /// <returns></returns>
        public static string[] QueryProjectTypeGuids(IVsHierarchy projectHierarchy)
        {
            var projectTypeGuids = string.Empty;

            var aggregatableProject = projectHierarchy as IVsAggregatableProject;
            if (aggregatableProject != null)
                aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);

            return projectTypeGuids.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Determines actual web project type from type guid. If type guid not supplied,
        /// retrieves from the project's IVsHierarchy instance.
        /// </summary>
        /// <param name="projectTypeGuid"></param>
        /// <remarks>Expects VsHierarchy to be set to valid IVsHierarchy instance if projectTypeGuid not supplied</remarks>
        void SetWebProjectType(string projectTypeGuid)
        {
            // coreclr projects have no type guid, so this fake guid allows us to handle the
            // different project type in roughly the same way
            if (projectTypeGuid.Equals(guidAWSPrivateCoreCLRWebProject, StringComparison.OrdinalIgnoreCase))
            {
                VsProjectType = VsWebProjectType.CoreCLRWebProject;
                return;
            }

            var typeGuid = projectTypeGuid;
            if (string.IsNullOrEmpty(typeGuid))
            {
                var typeGuids = QueryProjectTypeGuids(VsHierarchy);
                foreach (var guid in typeGuids)
                {
                    if (IsWebProjectType(guid))
                    {
                        typeGuid = guid;
                        break;
                    }
                }
            }

            VsProjectType = VsWebProjectKinds.ContainsKey(typeGuid) ? VsWebProjectKinds[typeGuid] : VsWebProjectType.NotWebProjectType;
        }

        string CachedProjectPathAndName
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedProjectPathAndName))
                {
                    try
                    {
                        // In vs2008/2010, the FullName property on the DTE project object
                        // for a website-type project yields the disk location. In VS2012,
                        // it yields a http://local host address. To support 2008/2010, try
                        // the original approach first and probe deeper if we need to on 2012.
                        // Web app project types behave consistently across all three versions
                        // in respect to the FullName property.
                        _cachedProjectPathAndName = DTEProject.FullName;
                        Logger.InfoFormat("EnvDTEProject.FullName lookup yielded '{0}'", _cachedProjectPathAndName);
                        if (VsProjectType == VsWebProjectType.WebSiteProject
                                && _cachedProjectPathAndName.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var props = DTEProject.Properties;
                            var fullPathFromProps = props.Item("FullPath").Value as string;
                            if (!string.IsNullOrEmpty(fullPathFromProps))
                            {
                                Logger.InfoFormat("Detected VS2012 HTTP-based path for website project, 'FullPath' properties lookup yielded '{0}'",
                                                    fullPathFromProps);

                                // this approach can also yield a folder name that is not terminated,
                                // which can further throw off the content location setting for the 
                                // msdeploy wrapper invocation, so force it
                                fullPathFromProps.TrimEnd('\\', '/');
                                _cachedProjectPathAndName = string.Concat(fullPathFromProps, Path.DirectorySeparatorChar);
                            }

                            Logger.InfoFormat("Website project location lookup final yield is {0}", _cachedProjectPathAndName);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Exception probing for website project folder location", e);                        
                    }
                }

                return _cachedProjectPathAndName;
            }
        }

        [Browsable(false)]
        public EnvDTEProject DTEProject
        {
            get
            {
                try
                {
                    object value;
                    VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out value);
                    return value as EnvDTEProject;
                }
                catch (Exception)
                {
                }

                return null;
            }
        }

        [Browsable(false)]
        public IEnumerable<string> BuildConfigurations
        {
            get
            {
                var rowNames = (Array) DTEProject.ConfigurationManager.ConfigurationRowNames;
                return rowNames.Cast<object>().Cast<string>().ToList();
            }
        }

        public string ActiveBuildConfiguration
        {
            get
            {
                if (DTEProject != null)
                    return DTEProject.ConfigurationManager.ActiveConfiguration.ConfigurationName;

                return string.Empty;
            }    
        }

        public MSBuildProject LoadMSBuildEvaluation()
        {
            if (VsProjectType == VsWebProjectType.NotWebProjectType)
            {
                Logger.Error("Called to load msbuild information but project type is not web app/web site.");
                System.Diagnostics.Debug.Assert(false);
                return null;
            }

            MSBuildProject evalProject = null;
            if (VsProjectType == VsWebProjectType.WebApplicationProject)
            {
                var projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(CachedProjectPathAndName);
                evalProject = projects.First<MSBuildProject>();
            }
            else
            {
                var generatedProject = SolutionWrapperProject.Generate(DTEProject.DTE.Solution.FullName, null, null);
                if (!string.IsNullOrEmpty(generatedProject))
                {
                    using (var reader = new XmlTextReader(new StringReader(generatedProject)))
                    {
                        evalProject = new MSBuildProject(reader);
                    }
                }
            }

            if (evalProject == null)
            {
                Logger.ErrorFormat("Failed to load {0} instance for project {1}", typeof(MSBuildProject), VsProjectLocationAndName);
                System.Diagnostics.Debug.Assert(false);
            }

            return evalProject;
        }
    }

    internal static class MSBuildExtensions
    {
        /// <summary>
        /// Extension method to factor out differences in reading project property values between msbuild apis
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string QueryPropertyValue(this MSBuildProject buildProject, string propertyName)
        {
            return buildProject.GetPropertyValue(propertyName);
        }
    }
}
