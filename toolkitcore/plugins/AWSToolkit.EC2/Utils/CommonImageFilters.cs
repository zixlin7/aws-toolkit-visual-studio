using System.Collections.Generic;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Utils
{
    public class CommonImageFilters
    {
        public static readonly CommonImageFilters OWNED_BY_ME = new CommonImageFilters("Owned By Me",
            new Filter() { Name = "owner-alias", Values = new List<string>() { "self" } });

        public static readonly CommonImageFilters ALL = new CommonImageFilters("All Images", 
            new Filter());

        public static readonly CommonImageFilters AMAZON = new CommonImageFilters("Amazon Images", 
            new Filter() { Name = "owner-alias", Values = new List<string>() { "amazon" } });

        public static readonly CommonImageFilters PUBLIC = new CommonImageFilters("Public Images",
            new Filter() { Name = "is-public", Values = new List<string>() { "true" } });

        public static readonly CommonImageFilters PRIVATE = new CommonImageFilters("Private Images",
            new Filter(){ Name = "is-public", Values = new List<string>() { "false" } });

        public static readonly CommonImageFilters EBS = new CommonImageFilters("EBS Images",
            new Filter() { Name = "root-device-type", Values = new List<string>() { "ebs" } });

        public static readonly CommonImageFilters INSTANCE_STORE = new CommonImageFilters("Instance-Store Images", 
            new Filter() { Name = "root-device-type", Values = new List<string>() { "instance-store" } });

        public static readonly CommonImageFilters BIT_32 = new CommonImageFilters("32-Bit Images",
            new Filter() { Name = "architecture", Values = new List<string>() { "i386" } });

        public static readonly CommonImageFilters BIT_64 = new CommonImageFilters("64-Bit Images",
            new Filter() { Name = "architecture", Values = new List<string>() { "x86_64" } });

        static CommonImageFilters[] _allFilters;

        public static IEnumerable<CommonImageFilters> AllFilters => _allFilters;

        static CommonImageFilters()
        {
            _allFilters = new CommonImageFilters[]
            {
                OWNED_BY_ME,
                ALL,
                AMAZON,
                PUBLIC,
                PRIVATE,
                EBS,
                INSTANCE_STORE,
                BIT_32,
                BIT_64
            };
        }


        private CommonImageFilters(string displayName, params Filter[] filters)
        {
            this.DisplayName = displayName;
            this.Filters = filters;
        }


        public string DisplayName
        {
            get;
        }

        public Filter[] Filters
        {
            get;
        }
    }
}
