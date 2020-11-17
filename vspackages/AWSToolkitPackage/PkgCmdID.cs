// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace Amazon.AWSToolkit.VisualStudio
{
    static class PkgCmdIDList
    {
        // command ids
        public const uint cmdidAWSNavigator                 = 0x101;
        public const uint cmdidPublishToAWS                 = 0x102;
        public const uint cmdIdRepublishToAWS               = 0x103;
        public const uint cmdidPublishContainerToAWS        = 0x104;
        public const uint cmdidCodeArtifactSelectProfile    = 0x105;

        public const uint cmdidDeployTemplateSolutionExplorer = 0x200;
        public const uint cmdidEstimateTemplateCostSolutionExplorer = 0x201;
        public const uint cmdidFormatTemplateSolutionExplorer = 0x202;
        public const uint cmdidDeployTemplateActiveDocument = 0x203;
        public const uint cmdidEstimateTemplateCostActiveDocument = 0x204;
        public const uint cmdidFormatTemplateActiveDocument = 0x205;
        public const uint cmdidAddCloudFormationTemplate = 0x206;
        public const uint cmdidAddAWSServerlessTemplate = 0x207;

        public const uint cmdidDeployToLambdaSolutionExplorer = 0x300;
        public const uint cmdidDeployToLambdaServerlessTemplate = 0x301;

        public const uint cmdidTeamExplorerConnect = 0x400;

        // Groups
        public const uint IDG_AWS_CLOUDFORMATION_DEPLOYMENT = 0x1005;
        public const uint IDG_AWS_CLOUDFORMATION_EDIT = 0x1010;

        public const uint IDG_TEAMEXPLORER_MANAGECONNECTIONS = 0x01050;
    };
}