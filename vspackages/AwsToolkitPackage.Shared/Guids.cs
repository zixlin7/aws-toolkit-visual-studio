// Guids.cs
// MUST match guids.h
using System;

namespace Amazon.AWSToolkit.VisualStudio
{
    static class GuidList
    {
        /// <summary>
        /// This Guid MUST stay in sync with VsixGuid in buildtools\Package.Build.targets
        ///
        /// If this repo needs to produce different Toolkit products in the future, ifdef could be used
        /// by setting up BuildConstants in buildtools\Common.Build.CSharp.settings
        ///
        /// Previous VS Toolkit GUIDs:
        /// VS 2013: 9510184f-8135-4f8a-ab8a-23be77c345e2
        /// VS 2015: f2884b07-5122-4e23-acd7-4d93df18709e
        /// VS 2017 (current Toolkit): 12ed248b-6d4a-47eb-be9e-8eabea0ff119
        /// VS 2022: 0B82CB16-0E52-4363-9BC0-61E758689176
        /// </summary>
#if VS2022
        public const string AwsToolkitPackageGuidString = Constants.ToolkitPackageGuids.Vs2022AsString;
#elif VS2017_OR_LATER
        public const string AwsToolkitPackageGuidString = Constants.ToolkitPackageGuids.Vs20172019AsString;
#endif

        public const string CommandSetGuidString = "8ba6f49c-ca32-4bc4-a71c-77b8503b93c2";
        public static readonly Guid CommandSetGuid = new Guid(CommandSetGuidString);

        //public const string ToolWindowPersistanceGuidString = "58d054c4-d113-4446-be66-a2f783515f53";

        public const string HostedEditorFactoryGuidString = "7b907bed-beab-4f29-b133-3bc88632e45a";
        public static readonly Guid HostedEditorFactoryGuid = new Guid(HostedEditorFactoryGuidString);

        public const string GeneralOptionsGuidString = "1d5126e6-09e4-46f2-a9e6-37397041aa47";
        public const string ProxyOptionsGuidString = "5924f985-a351-400a-aae9-dfe37b6837ed";

        public const string AWSExplorerToolWindowGuidString = "58d054c4-d113-4446-be66-a2f783515f53";

        public const string SAWSToolkitServiceGuidString = "b95300f6-10e0-4f28-8edf-00f087d6eebe";
        public const string IAWSToolkitServiceGuidString = "1401bed5-bf44-4f33-a835-04002d66781b";

        public const string guidCloudFormationTemplateProjectFactoryString = "22325025-cda3-4736-83cb-2d32d2fcc215";
        public static readonly Guid guidCloudFormationTemplateProjectFactory = new Guid(guidCloudFormationTemplateProjectFactoryString);

        public const string guidTemplateEditorFactoryString = "ec06ded3-700f-4ce3-9313-8e2324ba885d";
        public static readonly Guid guidTemplateEditorFactory = new Guid(guidTemplateEditorFactoryString);

        public static readonly Guid SHLMainMenuGuid = new Guid(0xd309f791, 0x903f, 0x11d0, 0x9e, 0xfc, 0x00, 0xa0, 0xc9, 0x11, 0x00, 0x4f);

        // Team Explorer integration
        public const string guidCodeCommitConnectSectionString = "d4632b03-6cf0-4f5e-9124-175280c955df";

        public const string VSProjectTypeProjectFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        public static readonly Guid MetricsOutputWindowPane = new Guid("9E07E6E4-24C1-4E8A-BE36-4E99E6882D61");

        // Source : Microsoft.VisualStudio.ImageCatalog
        // There are conflicts with .NET Framework versions when trying to 
        // directly reference the NuGet package Microsoft.VisualStudio.ImageCatalog
        // due to the VS2019 CodeCommit module using framework 4.7.x when the rest of the 
        // code uses 4.6.x (to support VS2017).
        // Instead, we'll place the values of interest here.
        // TODO : Get Microsoft.VisualStudio.ImageCatalog NuGet references working in-solution somehow
#region KnownImageIds

        public static class VsImageCatalog
        {
            public static readonly Guid ImageCatalogGuid = new Guid("{ae27a6b0-e345-4288-96df-5eaf394ee369}");
            public const int StatusInformation = 2933;
        }

#endregion
    };
}
