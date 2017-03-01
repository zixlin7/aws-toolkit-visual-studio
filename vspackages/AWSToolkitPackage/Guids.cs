// Guids.cs
// MUST match guids.h
using System;

namespace Amazon.AWSToolkit.VisualStudio
{
    static class GuidList
    {
#if VS2013
        public const string guidPackageString = "9510184f-8135-4f8a-ab8a-23be77c345e2";
#elif VS2015
        public const string guidPackageString = "f2884b07-5122-4e23-acd7-4d93df18709e";
#elif VS2017
        public const string guidPackageString = "12ed248b-6d4a-47eb-be9e-8eabea0ff119";
#else
#error "No VS20xx conditional defined - cannot assign guidPackageString (see package.build.targets)"
#endif

        public static readonly Guid guidPackage = new Guid(guidPackageString);

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

        public const string guidNodeJSConsoleProjectFactoryString = "{3AF33F2E-1136-4D97-BBB7-1795711AC8B8};{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";

        public static readonly Guid SHLMainMenuGuid = new Guid(0xd309f791, 0x903f, 0x11d0, 0x9e, 0xfc, 0x00, 0xa0, 0xc9, 0x11, 0x00, 0x4f);

    };
}
