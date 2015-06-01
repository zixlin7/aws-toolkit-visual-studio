// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace Amazon.AWSToolkit.VisualStudio
{
    static class PkgCmdIDList
    {
        // command ids
        public const uint cmdidAWSNavigator             = 0x101;
        public const uint cmdidPublishToAWS             = 0x102;
        public const uint cmdIdRepublishToAWS           = 0x103;

        public const uint cmdidDeployTemplateSolutionExplorer = 0x104;
        public const uint cmdidEstimateTemplateCostSolutionExplorer = 0x105;
        public const uint cmdidFormatTemplateSolutionExplorer = 0x106;
        public const uint cmdidDeployTemplateActiveDocument = 0x107;
        public const uint cmdidEstimateTemplateCostActiveDocument = 0x108;
        public const uint cmdidFormatTemplateActiveDocument = 0x109;
        public const uint cmdidAddCloudFormationTemplate = 0x110;

        public const uint cmdidDeployToLambdaSolutionExplorer = 0x111;

        // Menus
        public const uint IDM_AWS_MM_CLOUDFORMATION_TEMPLATE = 0x1000;

        // Groups
        public const uint IDG_AWS_CLOUDFORMATION_DEPLOYMENT  = 0x1005;
        public const uint IDG_AWS_CLOUDFORMATION_EDIT  = 0x1010;
    };
}