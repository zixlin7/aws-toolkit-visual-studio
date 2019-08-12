namespace Amazon.AWSToolkit.EC2
{
    public static class EC2Constants
    {
        public const string NO_PUBLIC_IP_CONFIRM_CONNECT_PRIVATE_IP =
                    "This EC2 instance is in a VPC and does not have a public DNS or Elastic IP Address. \r\n\r\n" +
                    "To connect to an EC2 instance inside a VPC you should associate an Elastic IP address. " +
                    "If you are currently connecting through a VPN to your VPC you can attempt to connect with the private IP address. \r\n\r\n" +
                    "Do you want to attempt to connect using the private IP address?";

        public const string INSTANCE_STATE_PENDING = "pending";
        public const string INSTANCE_STATE_RUNNING = "running";
        public const string INSTANCE_STATE_SHUTTING_DOWN = "shutting-down";
        public const string INSTANCE_STATE_TERMINATED = "terminated";
        public const string INSTANCE_STATE_STOPPING = "stopping";
        public const string INSTANCE_STATE_STOPPED = "stopped";

        public const string VOLUME_STATE_IN_USE = "in-use";
        public const string VOLUME_STATE_AVAILABLE = "available";
        public const string VOLUME_STATE_DELETING = "deleting";
        public const string VOLUME_STATE_CREATING = "creating";

        public const string VOLUME_ATTACTMENT_STATUS_ATTACHING = "attaching";
        public const string VOLUME_ATTACTMENT_STATUS_ATTACHED = "attached";
        public const string VOLUME_ATTACTMENT_STATUS_DETACHING = "detaching";
        public const string VOLUME_ATTACTMENT_STATUS_DETACHED = "detached";

        public const string SNAPSHOT_STATUS_PENDING = "pending";
        public const string SNAPSHOT_STATUS_ERROR = "error";
        public const string SNAPSHOT_STATUS_COMPLETED = "completed";

        public const string IMAGE_STATE_AVAILABLE = "available";
        public const string IMAGE_STATE_PENDING = "pending";
        public const string IMAGE_STATE_FAILED = "failed";

        public const string VPC_STATE_AVAILABLE = "available";
        public const string VPC_STATE_PENDING = "pending";

        public const string SUBNET_STATE_AVAILABLE = "available";
        public const string SUBNET_STATE_PENDING = "pending";

        public const string IMAGE_VISIBILITY_PUBLIC = "Public";
        public const string IMAGE_VISIBILITY_PRIVATE = "Private";

        public const string PLATFORM_WINDOWS = "windows";

        public const string ROOT_DEVICE_TYPE_EBS = "ebs";

        public const int TIME_BEFORE_DETECT_NOPASSWORD = 30;
        public const string DEFAULT_ADMIN_USER = "Administrator";

        public const int SELECTED_ITEMS_NAME_LENGTH = 50;

        public const string RESULTS_PARAMS_NEWIDS = "RESULTS_PARAMS_NEWIDS";

        public const string TAG_NAME = "Name";

        public enum PermissionType { Ingress, Egrees };

        public const string VPC_LAUNCH_PUBLIC_SUBNET_NAME = "Public";
        public const string VPC_LAUNCH_PRIVATE_SUBNET_NAME = "Private";
        public const string VPC_LAUNCH_NAT_GROUP = "NATGroup";
    }
}
