using System.Runtime.Versioning;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AwsToolkit.VsSdk.Common
{
    public static class VsHierarchyHelpers
    {
        /// <summary>
        /// Wrap the <see cref="IVsHierarchy"/> GetProperty call,
        /// to handle return values and casting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hierarchy">Hierarchy to query</param>
        /// <param name="itemId">Id of item in hierarchy. See <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivshierarchy.getproperty"/></param>
        /// <param name="propertyId">See <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.__vshpropid"/></param>
        /// <param name="value">Retrieved property</param>
        public static bool TryGetItemProperty<T>(
                this IVsHierarchy hierarchy,
                uint itemId, int propertyId,
                out T value)
        {
            value = default(T);

            if (hierarchy == null)
            {
                return false;
            }

            var result = hierarchy.GetProperty(itemId, propertyId, out object propertyValue);

            if (result != VSConstants.S_OK)
            {
                return false;
            }

            if (!(propertyValue is T typedValue))
            {
                return false;
            }

            value = typedValue;
            return true;
        }

        /// <summary>
        /// Retrieve the ExtObject for the specified hierarchy.
        /// Typically used to get the EnvDTE related Solution Explorer item
        /// </summary>
        /// <param name="hierarchy">Hierarchy to query</param>
        /// <param name="itemId">Id of item in hierarchy. See <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivshierarchy.getproperty"/></param>
        /// <param name="extObject">Requested ExtObject</param>
        public static bool TryGetExtObject(IVsHierarchy hierarchy, uint itemId, out object extObject)
        {
            return hierarchy.TryGetItemProperty(itemId, (int) __VSHPROPID.VSHPROPID_ExtObject, out extObject);
        }

        /// <summary>
        /// Attempts to return the project represented by <see cref="hierarchy"/>, else the project
        /// that contains it.
        /// </summary>
        public static bool TryResolvingToProject(IVsHierarchy hierarchy, uint itemId, out EnvDTE.Project project)
        {
            if (TryGetExtObject(hierarchy, itemId, out object extObject))
            {
                switch (extObject)
                {
                    case EnvDTE.Project asProject:
                        project = asProject;
                        return true;
                    case EnvDTE.ProjectItem projectItem:
                        // See if we can get the Project associated with the item that is selected
                        project = projectItem.ContainingProject;
                        return true;
                }
            }

            project = null;
            return false;
        }

        /// <summary>
        /// Queries the specified hierarchy for the targeted framework (like .NET Framework / Core)
        /// </summary>
        /// <param name="hierarchy">Hierarchy to query</param>
        /// <param name="itemId">Id of item in hierarchy. See <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivshierarchy.getproperty"/></param>
        /// <param name="targetFramework">Requested target framework</param>
        public static bool TryGetTargetFramework(IVsHierarchy hierarchy, uint itemId, out FrameworkName targetFramework)
        {
            if (!hierarchy.TryGetItemProperty(itemId, (int) __VSHPROPID4.VSHPROPID_TargetFrameworkMoniker,
                out string targetFrameworkMoniker))
            {
                targetFramework = null;
                return false;
            }

            // Example framework monikers:
            // .NETFramework,Version=v4.7.2
            // .NETCoreApp,Version=v5.0
            targetFramework = new FrameworkName(targetFrameworkMoniker);
            return true;
        }
    }
}
