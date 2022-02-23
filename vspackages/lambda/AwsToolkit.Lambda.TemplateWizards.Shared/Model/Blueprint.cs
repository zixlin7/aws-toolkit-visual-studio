using Amazon.AWSToolkit.VersionInfo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Model
{
    /// <summary>
    /// Details a blueprint that can be selected by the user to control
    /// the new C# Lambda function we generate for them.
    /// </summary>
    public class Blueprint
    {
        /// <summary>
        /// The display name of the blueprint.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of what the generated code will do.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the zip file containing the blueprint content.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The sort order to display displayed in the wizard
        /// </summary>
        public int SortOrder { get; set; }

        [XmlArray]
        [XmlArrayItem("Tag")]
        public HashSet<string> Tags { get; set; }

        [XmlArray]
        [XmlArrayItem("HiddenTag")]
        public HashSet<string> HiddenTags { get; set; }

        /// <summary>
        /// The minimum toolkit version that this blue print is capatible for.
        /// </summary>
        public string MinToolkitVersion { get; set; }

        /// <summary>
        /// If set true, the blueprint does not appear in the selection panel
        /// at the bottom of the wizard. Use this to mask blueprints that map
        /// to buttons on the wizard.
        /// </summary>
        public bool IsHidden => this.HiddenTags != null && this.HiddenTags.Contains("hidden", StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the blueprint has a matching tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool HasAnyTagsFromSet(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (this.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the blueprint has a matching tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool HasRequiredHiddenTagFromSet(IEnumerable<string> tags)
        {
            if (this.HiddenTags == null || this.HiddenTags.Count == 0)
                return false;

            foreach (var tag in tags)
            {
                if (!this.HiddenTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Optional additional tooltip that can be shown when the user hovers
        /// the mouse over the bluetip or option button mapped to the blueprint.
        /// </summary>
        public string Tooltip { get; set; }

        public string TagCollection
        {
            get
            {
                if (!Tags.Any())
                    return string.Empty;

                return string.Join(", ", Tags);
            }
            set => Tags = new HashSet<string>(value.Split(','), StringComparer.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// Contains the set of blueprints available for the user to select
    /// from. At runtime this collection is loaded from a manifest contained
    /// in our hosted files location.
    /// </summary>
    [XmlRoot("BlueprintManifest")]
    public class BlueprintsModel
    {
        public string ManifestVersion { get; set; }


        /// <summary>
        /// All blueprints we read from the backing store
        /// </summary>
        [XmlArray("Blueprints")]
        [XmlArrayItem("Blueprint")]
        public List<Blueprint> AllBlueprints { get; set; }

        /// <summary>
        /// Returns the collection of filters that matches one of the supplied filter tags.
        /// </summary>
        /// <param name="filters">Tags to filter on; pass null get all user-visible blueprints returned.</param>
        /// <returns></returns>
        internal IEnumerable<Blueprint> BlueprintsFromFilter(IEnumerable<string> requiredHiddenTags, IEnumerable<string> optionalTagFilters)
        {
            var output = new List<Blueprint>();
            foreach (var blueprint in AllBlueprints)
            {
                if (blueprint.IsHidden)
                    continue;

                if (!string.IsNullOrEmpty(blueprint.MinToolkitVersion) && VersionManager.IsVersionGreaterThanToolkit(blueprint.MinToolkitVersion))
                    continue;

                bool hasHiddenTags = blueprint.HasRequiredHiddenTagFromSet(requiredHiddenTags);

                if (hasHiddenTags)
                {
                    if (optionalTagFilters == null)
                        output.Add(blueprint);
                    else
                    {
                        if (blueprint.HasAnyTagsFromSet(optionalTagFilters))
                        {
                            output.Add(blueprint);
                        }
                    }
                }
            }

            output.Sort((x, y) =>
            {
                if (x.SortOrder == y.SortOrder)
                    return string.Compare(x.Name, y.Name);
                else if (x.SortOrder < y.SortOrder)
                    return -1;
                else
                    return 1;
            });

            return output;
        }

        /// <summary>
        /// Returns the blueprint corresponding to the supplied name, or null if
        /// not found.
        /// </summary>
        /// <param name="name">The name of the blueprint</param>
        /// <returns>Blueprint instance or null</returns>
        internal Blueprint BlueprintFromName(string name)
        {
            foreach(var blueprint in AllBlueprints)
            {
                if (blueprint.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return blueprint;
            }

            return null;
        }

        internal BlueprintsModel()
        {
        }
    }

    internal abstract class BlueprintsManifest
    {
        public static readonly string BlueprintsManifestPathV1 = @"LambdaSampleFunctions\NETCore\v1\";

        private static readonly string BlueprintsManifestPathMsbuildStyle = @"LambdaSampleFunctions\NETCore\msbuild-v9";
        public static readonly string BlueprintsManifestFile = "vs-lambda-blueprint-manifest.xml";

        public static string GetBlueprintManifest(string blueprintTypes)
        {
            return Path.Combine(blueprintTypes, BlueprintsManifestFile);
        }

        public static string GetBlueprintsPath(string vsVersion)
        {
            return $"{BlueprintsManifestPathMsbuildStyle}/vs{vsVersion}";
        }

        public static BlueprintsModel Deserialize(string blueprintTypes)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(BlueprintsModel));
                using (var fs = S3FileFetcher.Instance.OpenFileStream(GetBlueprintManifest(blueprintTypes), S3FileFetcher.CacheMode.PerInstance))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        return (BlueprintsModel)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Unable to retrieve content for file " + GetBlueprintManifest(blueprintTypes), e);
            }
        }
    }

    public static class BlueprintSampleDataModel
    {
        public class SampleDataContext
        {
            public static ObservableCollection<Blueprint> SelectableBlueprints
            {
                get
                {
                    var c = new ObservableCollection<Blueprint>();

                    // use lots of 'cards' so we can test layout flows
                    c.Add(new Blueprint
                    {
                        Name = "S3 Event Notification",
                        Description = "Handles an S3 event",
                        HiddenTags = new HashSet<string> { "C#"},
                        TagCollection = "C#,S3,Event,hidden",
                        File="S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "Custom Data Object",
                        Description = "Handles a custom data object",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,poco,custom,hidden",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "a-blueprint",
                        Description = "Yadda yadda",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,yadda,dynamodb",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "another-blueprint",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,gateway,dabba,blue",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "blue-sky-blueprint",
                        Description = "Beeyootiful day",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,sky,blue",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint",
                        Description = "Yabba dabba doo!",
                        TagCollection = "C#,awesomeness,defined",
                        HiddenTags = new HashSet<string> { "C#" },
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint2",
                        Description = "Yabba dabba doo!",
                        TagCollection = "C#,awesomeness,defined",
                        HiddenTags = new HashSet<string> { "C#" },
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint3",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint4",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint5",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint6",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint7",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    c.Add(new Blueprint
                    {
                        Name = "one-more-blueprint8",
                        Description = "Yabba dabba doo!",
                        HiddenTags = new HashSet<string> { "C#" },
                        TagCollection = "C#,awesomeness,defined",
                        File = "S3Event.zip"
                    });

                    return c;
                }
            }
        }

        public static SampleDataContext SampleData => new SampleDataContext();
    }
}
