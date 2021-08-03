using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Utilities.DTE
{
    public static class ProjectExtensionMethods
    {
        public static string SafeGetFileName(this Project project, string defaultValue = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return SafeGetProperty(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.FileName;
                },
                defaultValue);
        }

        public static string SafeGetFullName(this Project project, string defaultValue = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return SafeGetProperty(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.FullName;
                },
                defaultValue);
        }

        public static string SafeGetUniqueName(this Project project, string defaultValue = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return SafeGetProperty(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.UniqueName;
                },
                defaultValue);
        }

        private static T SafeGetProperty<T>(Func<T> getter, T defaultValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return getter();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}