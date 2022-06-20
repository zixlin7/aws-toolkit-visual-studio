using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.S3.IO
{
    /// <summary>
    /// Provides methods for processing, validating, and composing S3 paths.
    /// </summary>
    /// <remarks>
    /// This class was modeled on the <see cref="System.IO.Path"/> built-in .NET class.  See it's remarks and similar methods
    /// for further understanding and motivations.
    /// 
    /// See https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-keys.html" for details on S3 path semantics.
    /// </remarks>
    public static class S3Path
    {
        // Implementation Assumption(s):
        // If these assumptions ever change, the whole class will need review and revision!
        //
        //   1. S3 paths do not have a leading directory separator.
        //   2. Directory names must always end with a directory separator.
        //   3. Root isn't considered a directory.
        //   4. No-name (empty string) directories are allowed.  As other directories, they are terminated by a directory separator.
        //   5. Root is an empty string, no-name directories are an empty string followed by a directory separator, effectively just a directory separator.
        //   6. Public methods that use a directory separator must accept an optional directorySeparator parameter set to DefaultDirectorySeparator by default.
        //   6. The directorySeparator parameter should be the last optional parameter defined in the method as it is least likely to change.
        //   7. Private methods that use a directorySeparator must have a required directorySeparator parameter.
        //   8. "path" is assumed to be an absolute path starting at Root.

        /// <summary>
        /// The root for all S3 paths.
        /// </summary>
        public const string Root = "";

        /// <summary>
        /// The default directory separator for S3 paths as defined in the S3 User Guide.
        /// </summary>
        /// <remarks>
        /// While the default S3 path should be used in new and compliant existing implementations, the methods of this
        /// class do support use of alternate directory separators.
        /// </remarks>
        public const string DefaultDirectorySeparator = "/";

        /// <summary>
        /// Values that are not supported for use as directory separators.
        /// </summary>
        /// <remarks>
        /// All methods will throw an ArgumentException if any of these values are passed as a directory separator.
        /// </remarks>
        public static readonly string[] InvalidDirectorySeparators = {null, ""};

        /// <summary>
        /// Combines an absolute path with relative path(s) into a single path using the DefaultDirectorySeparator.
        /// </summary>
        /// <remarks>
        /// See <see cref="Amazon.S3.IO.S3Path.CombineWithDirectorySeparator" /> Remarks for further details.
        /// </remarks>
        public static string Combine(params string[] paths)
        {
            return CombineWithDirectorySeparator(DefaultDirectorySeparator, paths);
        }

        /// <summary>
        /// Combines an absolute path with relative path(s) into a single path.
        /// </summary>
        /// <remarks>
        /// See <see cref="System.IO.Path.Combine(string[])"/> remarks for a general overview of path combining.
        ///
        /// As S3 paths don't contain any special characters/separators for root, it's impossible to discern an absolute path
        /// from a relative path.  The first path provided is assumed to be an absolute path.  Subsequent paths are assumed to
        /// be relative paths.  This doesn't necessarily impact most handling, but be aware that an absolute path of root as the
        /// first argument is not terminated with a directory separator as this would inject a no-name directory at root.  However,
        /// if an empty string is passed for any subsequent relative path arguments, they are assumed to be no-name directories
        /// (as relative paths would not contain root) and are terminated with a directory separator and injected into the resulting
        /// path.
        ///
        /// Leading directory separators indicate no-name directories and are not trimmed. For all but the last path component,
        /// only the last trailing directory separator, if any, is trimmed.  Any additional trailing directory separators are
        /// retained and assumed to be no-name directories.
        ///
        /// Whitespace is retained and not treated specially.  This is unlike the Path.Combine() method that handles whitespace
        /// differently, likely due to how Windows handles escaping whitespace in unquoted string paths.
        ///
        /// If there is overlap
        /// between the trailing end of one path and the leading end of the next path, no effort is made to deduplicate these.
        /// For example, "a/b" and "b/c" will result in "a/b/b/c" being returned.  As it is possible that both a/b/c and a/b/b/c
        /// directories exist, there is no deterministic way for this method to correctly determine the result.
        ///
        /// If any path component is null, null is returned.
        /// </remarks>
        /// <param name="directorySeparator"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string CombineWithDirectorySeparator(string directorySeparator, params string[] paths)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (paths.Length == 0 || paths.Any(p => p == null))
            {
                return null;
            }

            if (IsRoot(paths[0]))
            {
                if (paths.Length == 1)
                {
                    return Root;
                }

                // Skip root as we don't want String.Join injecting a leading directory separator for it
                paths = paths.Skip(1).ToArray();
            }

            // Don't strip ending directory separator off of last path so if it was a directory, it stays a directory
            for (var i = 0; i < paths.Length - 1; ++i)
            {
                paths[i] = TrimEndingDirectorySeparator(paths[i], directorySeparator: directorySeparator);
            }

            return string.Join(directorySeparator, paths);
        }

        /// <summary>
        /// Returns the relative path remaining in <param name="path">path</param> after removing the leading
        /// <param name="basePath">basePath</param>.
        /// </summary>
        /// <param name="basePath">An absolute path that must be the leading path of the path parameter.</param>
        /// <param name="path">The path to remove the basePath from.</param>
        /// <param name="directorySeparator">The directory separator used in the paths.</param>
        /// <returns>The relative path after basePath in removed from path.  Otherwise, the value of the path argument if any path
        /// arguments are null/root or path isn't prefixed with basePath.</returns>
        public static string GetRelativePath(string basePath, string path, string directorySeparator = DefaultDirectorySeparator)
        {
            if (basePath == null || IsRoot(basePath) || !IsDescendant(basePath, path, directorySeparator))
            {
                return path;
            }

            return path.Substring(basePath.Length);
        }

        /// <summary>
        /// Returns the path with filename removed if present.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>Directory portion of path.</returns>
        public static string GetDirectoryPath(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null || IsRoot(path) || IsDirectory(path, directorySeparator))
            {
                return path;
            }

            return path.Substring(0, path.Length - GetFileName(path, directorySeparator).Length);
        }

        /// <summary>
        /// Returns the owner of the directory or file provided in the supplied path.
        /// </summary>
        /// <param name="path">Path to return the owner of.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>The directory or root that owns the last component of the path provided.  The path argument is
        /// returned if path is null or root.</returns>
        public static string GetParentPath(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null || IsRoot(path))
            {
                return path;
            }

            return GetDirectoryPath(TrimEndingDirectorySeparator(path, directorySeparator: directorySeparator), directorySeparator);
        }

        /// <summary>
        /// Returns an enumerable of each component of the path.
        /// </summary>
        /// <param name="path">Path to enumerate.</param>
        /// <param name="includeRoot">Indicates whether root should be returned as the first item of the enumerable or not.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>An enumerable of each component of the path.</returns>
        public static IEnumerable<string> GetPathComponents(string path, bool includeRoot = true, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            // Using local function as a workaround so exception above can be thrown immediately instead
            // of after enumeration has started due to the use of the yield statement.
            IEnumerable<string> CreateEnumerable()
            {
                if (path == null)
                {
                    yield break;
                }

                if (includeRoot)
                {
                    yield return Root;
                }

                if (IsRoot(path))
                {
                    yield break;
                }

                var rawComponents = path.Split(new[] {directorySeparator}, StringSplitOptions.None);
                var lastComponentIndex = rawComponents.Length - 1;

                if (lastComponentIndex == 0) // It's just a file at the root
                {
                    yield return rawComponents[0];
                    yield break;
                }

                for (var i = 0; i < lastComponentIndex; ++i) // Return all directories up to the penultimate component
                {
                    yield return ToDirectory(rawComponents[i], directorySeparator);
                }

                if (rawComponents[lastComponentIndex] != string.Empty) // It's a file path
                {
                    yield return rawComponents[lastComponentIndex];
                }
            }

            return CreateEnumerable();
        }

        /// <summary>
        /// Returns the last file or directory of the path.
        /// </summary>
        /// <param name="path">The path to return the last component of.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <remarks>
        /// This method should be used when only the last component of a path is needed as it has better performance
        /// than GetPathComponents().Last() does.
        /// </remarks>
        /// <returns>The last file or directory of the path.  Otherwise, the supplied path if it is null or root.</returns>
        public static string GetLastPathComponent(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null || IsRoot(path))
            {
                return path;
            }

            return path.Substring(GetParentPath(path, directorySeparator).Length);
        }

        /// <summary>
        /// Returns the filename of the supplied path.
        /// </summary>
        /// <param name="path">The path to return the filename of.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>The file portion of the path.  Otherwise, null if path doesn't contain a file.</returns>
        public static string GetFileName(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null || IsRoot(path) || IsDirectory(path, directorySeparator))
            {
                return null;
            }

            var parsingPath = ToParsingPath(path, directorySeparator);
            return parsingPath.Substring(parsingPath.LastIndexOf(directorySeparator) + directorySeparator.Length);
        }

        /// <summary>
        /// Indicates if a path is to a directory, not a file.
        /// </summary>
        /// <param name="path">The path to check if it is a directory.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>True if the path is a directory, otherwise false.</returns>
        public static bool IsDirectory(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);
            return path != null && !IsRoot(path) && path.EndsWith(directorySeparator);
        }

        /// <summary>
        /// Indicates if a path is to a file, not a directory.
        /// </summary>
        /// <param name="path">The path to check if it is a file.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>True if the path is a file, otherwise false.</returns>
        public static bool IsFile(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);
            return !(path == null || IsRoot(path) || IsDirectory(path, directorySeparator));
        }

        /// <summary>
        /// Indicates if a path is root.
        /// </summary>
        /// <param name="path">The path to check if it is root.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>True if the path is root, otherwise false.</returns>
        public static bool IsRoot(string path)
        {
            return path == Root;
        }

        /// <summary>
        /// Indicates in the path is a descendant of the basePath.
        /// </summary>
        /// <param name="basePath">The ancestor path to be checked.</param>
        /// <param name="path">The path to determine if it is a descendant of the basePath.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>True if path is a descendant of basePath.  Otherwise, returns false.</returns>
        public static bool IsDescendant(string basePath, string path, string directorySeparator = DefaultDirectorySeparator)
        {
            return basePath != null && path != null && IsDirectory(basePath, directorySeparator) && basePath != path && path.StartsWith(basePath);
        }

        /// <summary>
        /// Converts a path to a directory if it isn't already a directory.
        /// </summary>
        /// <remarks>
        /// Be careful as both root and no-name directories appear the same, i.e. an empty string.  Both
        /// will be returned with a terminating directorySeparator, appearing to be an no-name directory.
        /// </remarks>
        /// <param name="path">Path to convert to a directory.</param>
        /// <param name="directorySeparator">Directory separator to use, if not supplied, the DefaultDirectorySeparator is used.</param>
        /// <returns>The path supplied as a directory path.  If path is null, returns null.</returns>
        public static string ToDirectory(string path, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null)
            {
                return null;
            }

            return IsDirectory(path, directorySeparator) ? path : $"{path}{directorySeparator}";
        }

        /// <summary>
        /// Trims the ending directory separator(s) from the path.
        /// </summary>
        /// <param name="path">The path to trim the directory separators from.</param>
        /// <param name="trimAll">Indicates if all ending directory separators should be trimmed or just the last one.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>The path with the ending directory separator(s) trimmed.</returns>
        public static string TrimEndingDirectorySeparator(string path, bool trimAll = false, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null)
            {
                return null;
            }

            if (!path.EndsWith(directorySeparator))
            {
                return path;
            }

            do
            {
                path = path.Substring(0, path.Length - directorySeparator.Length);
            } while (trimAll && path.EndsWith(directorySeparator));
            
            return path;
        }

        /// <summary>
        /// Trims the starting directory separator(s) from the path.
        /// </summary>
        /// <param name="path">The path to trim the directory separators from.</param>
        /// <param name="trimAll">Indicates if all starting directory separators should be trimmed or just the first one.</param>
        /// <param name="directorySeparator">directory separator to use if not DefaultDirectorySeparator.</param>
        /// <returns>The path with the starting directory separator(s) trimmed.</returns>
        public static string TrimStartingDirectorySeparator(string path, bool trimAll = false, string directorySeparator = DefaultDirectorySeparator)
        {
            ThrowOnInvalidDirectorySeparator(directorySeparator);

            if (path == null)
            {
                return null;
            }

            if (!path.StartsWith(directorySeparator))
            {
                return path;
            }

            do
            {
                path = path.Substring(directorySeparator.Length);
            } while (trimAll && path.StartsWith(directorySeparator));

            return path;
        }

        private static string ToParsingPath(string path, string directorySeparator)
        {
            // Prepend a directory separator so root is separated from the rest of the path for simpler parsing logic.
            return $"{directorySeparator}{path}";
        }

        private static void ThrowOnInvalidDirectorySeparator(string directorySeparator)
        {
            if (InvalidDirectorySeparators.Contains(directorySeparator))
            {
                throw new ArgumentException(nameof(directorySeparator), $"Cannot be in [{string.Join(", ", InvalidDirectorySeparators)}].");
            }
        }
    }
}
