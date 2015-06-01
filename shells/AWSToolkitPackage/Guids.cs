// Guids.cs
// MUST match guids.h
using System;

namespace Amazon.AWSToolkit.VisualStudio
{
    static class GuidList
    {
        public const string guid_VSPackageString = "9F4942A3-6E43-408B-95B7-4EB3F346E7B8";
        public const string guid_VSPackageCmdSetString = "8ba6f49c-ca32-4bc4-a71c-77b8503b93c2";
        public const string guidToolWindowPersistanceString = "58d054c4-d113-4446-be66-a2f783515f53";
        public const string guid_VSPackageEditorFactoryString = "7b907bed-beab-4f29-b133-3bc88632e45a";

        public const string guid_VSPackageGeneralOptionsString = "1D5126E6-09E4-46F2-A9E6-37397041AA47";
        public const string guid_VSPackageProxyOptionsString = "5924F985-A351-400A-AAE9-DFE37B6837ED";

        public static readonly Guid guid_VSPackageCmdSet = new Guid(guid_VSPackageCmdSetString);
        public static readonly Guid guid_VSPackageEditorFactory = new Guid(guid_VSPackageEditorFactoryString);

        public const string guidCloudFormationTemplateProjectFactoryString = "22325025-CDA3-4736-83CB-2D32D2FCC215";
        public static readonly Guid guidCloudFormationTemplateProjectFactory = new Guid(guidCloudFormationTemplateProjectFactoryString);

        public const string guidNodeJSConsoleProjectFactoryString = "{3AF33F2E-1136-4D97-BBB7-1795711AC8B8};{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";

        public static readonly Guid guidSHLMainMenu = new Guid(0xd309f791, 0x903f, 0x11d0, 0x9e, 0xfc, 0x00, 0xa0, 0xc9, 0x11, 0x00, 0x4f);
    };
}