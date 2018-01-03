using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Lambda.WizardPages
{
    /// <summary>
    /// Property keys used with the Upload to Lambda wizard. 
    /// </summary>
    public static class UploadFunctionWizardProperties
    {
        /// <summary>
        /// Indicates where the wizard was launched from.
        /// Type: UploadFunctionController.UploadOriginator
        /// </summary>
        public static readonly string UploadOriginator = "uploadOriginator";

        /// <summary>
        /// Controls whether we show generic or .Net Core specific details page
        /// on wizard launch.
        /// Type: UploadFunctionController.DeploymentType
        /// </summary>
        public static readonly string DeploymentType = "deploymentType";

        /// <summary>
        /// If invoking from a function view, a correctly account and region bound
        /// Lambda client to use.
        /// </summary>
        public static readonly string LambdaClient = "lambdaClient";

        /// <summary>
        /// The user account selected by the user (if control present) to own the
        /// uploaded function. This can also be used to select an account on entry
        /// to the wizard.
        /// Type: AccountViewModel.
        /// </summary>
        public static readonly string UserAccount = "userAccount";

        /// <summary>
        /// The region to host the function (if control present). This can also be
        /// used to select a region on entry to the wizard.
        /// Type: RegionEndpointsManager.RegionEndpoints.
        /// </summary>
        public static readonly string Region = "region";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string FunctionName = "functionName";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Description = "description";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Handler = "handler";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string SourcePath = "sourcePath";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string SaveSettings = "saveSettings";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Configuration = "configuration";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Framework = "framework";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string Runtime = "runtime";

        /// <summary>
        /// The selected IAM role for the function to assume when making calls to AWS.
        /// Type: IAMCapabilityPicker.IAMEntity.
        /// </summary>
        public static readonly string Role = "role";

        /// <summary>
        /// The set of managed policies for the function.
        /// Type: ICollection of IAMCapabilityPicker.PolicyTemplate
        /// </summary>
        public static readonly string ManagedPolicy = "managedPolicy";

        /// <summary>
        /// The user selected memory size for the function, in MB.
        /// Type: int.
        /// </summary>
        public static readonly string MemorySize = "memorySize";

        /// <summary>
        /// The user selected timeout for the function, in seconds.
        /// Type: int.
        /// </summary>
        public static readonly string Timeout = "timeout";

        /// <summary>
        /// One or more subnets to deploy into, or null if none were selected.
        /// Type: IEnumerable of SubnetWrapper
        /// </summary>
        public static readonly string Subnets = "subnets";

        /// <summary>
        /// The seed ids used pre select subnet in the control. These are not values passed along to the deployment
        /// command.
        /// </summary>
        public static readonly string SeedSubnetIds = "seedSubnetsIds";

        /// <summary>
        /// Security groups to match the subnet, if any.
        /// Type: IEnumerable of SecurityGroupWrapper.
        /// </summary>
        public static readonly string SecurityGroups = "securityGroups";

        /// <summary>
        /// The seed ids used pre select security group in the control. These are not values passed along to the deployment
        /// command.
        /// </summary>
        public static readonly string SeedSecurityGroupIds = "seedSecurityGroupsIds";

        /// <summary>
        /// Collection of environment variables to set in the environment.
        /// Type: IEnumerable of EnvironmentVariable
        /// </summary>
        public static readonly string EnvironmentVariables = "environmentVariables";

        /// <summary>
        /// Type: IDictionary<string, IList<string>>
        /// </summary>
        public static readonly string SuggestedMethods = "suggestedMethods";

        /// <summary>
        /// KMS key to use to protect environment variables at rest.
        /// Type: Amazon.KeyManagementService.Model.KeyListEntry.
        /// If null/not set, the user has elected to use the default service key
        /// (which in Lambda's api means just don't send a key on the api call).
        /// </summary>
        public static readonly string KMSKey = "kmsKey";

        /// <summary>
        /// Indicates the success or fail (= user closed) status of the wizard. Used
        /// because the 'review' page of the wizard does the actual upload and on
        /// success, if the auto-close-wizard option is set, invokes CancelRun to
        /// actually shut down the UI which in turn returns 'false' as the output
        /// from the wizard's Run() method.
        /// Type: Boolean.
        /// </summary>
        public static readonly string WizardResult = "wizardResult";

        public const string CloudFormationTemplate = "cloudformationTemplate";
        public const string CloudFormationParameters = "cloudFormationTemplateParameters";
        public const string StackName = "stackName";
        public const string IsNewStack = "IsNewStack";
        public const string S3Bucket = "s3Bucket";
        public const string S3Prefix = "s3Prefix";
        public const string CloudFormationTemplateWrapper = "CloudFormationTemplateWrapper";
        public const string CloudFormationTemplateParameters = "CloudFormationTemplateParameters";

        public const string ProjectTargetFrameworks = "ProjectTargetFrameworks";


    }
}
