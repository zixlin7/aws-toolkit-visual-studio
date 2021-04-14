namespace Amazon.AWSToolkit.CloudFormation
{
    public static class CloudFormationDeploymentWizardProperties
    {
        public static class SelectTemplateProperties
        {
            /// <summary>
            /// Bool, file is from local disk
            /// </summary>
            public static readonly string propkey_UseLocalTemplateFile = "useLocalTemplateFile";

            public static readonly string propKey_DisableLoadPreviousValues = "propKey_DisableLoadPreviousValues";
        }

        public static class SelectStackProperties
        {
            /// <summary>
            /// The AccountViewModel that is selected for deployment
            /// </summary>
            public static readonly string propkey_SelectedAccount = "propkey_SelectedAccount";

            /// <summary>
            /// The ToolkitRegion that is selected for deployment
            /// </summary>
            public static readonly string propkey_SelectedRegion = "propkey_SelectedRegion";

            /// <summary>
            /// True to create stack or false for update stack
            /// </summary>
            public static readonly string propkey_CreateStackMode = "propkey_CreateStackMode";
        }

        public static class TemplateParametersProperties
        {
            /// <summary>
            /// Dictionary<string,string>, parameter name -> paramter value for all the parameters
            /// </summary>
            public static readonly string propkey_TemplateParameterValues = "templateParameterValues";
        }

        public static class AWSOptionsProperties
        {
            /// <summary>
            /// If using the default toolkit containers, contains the ami id of the selected container
            /// (== windows 2008/2012 selector)
            /// </summary>
            public static readonly string propkey_ContainerAMI = "containerAMI";

            /// <summary>
            /// For wizard display purposes, holds the display name of the container pertaining to
            /// the ami selection
            /// </summary>
            public static readonly string propkey_ContainerName = "containerName";

            /// <summary>
            /// String for the topic
            /// </summary>
            public static readonly string propkey_SNSTopic = "snsTopic";

            /// <summary>
            /// Int for timeout.  If timeout is not used this will be set to -1.
            /// </summary>
            public static readonly string propkey_CreationTimeout = "creationTimeout";

            /// <summary>
            /// Bool for rolloback on failure
            /// </summary>
            public static readonly string propkey_RollbackOnFailure = "rollbackOnFailure";
        }
    }
}
