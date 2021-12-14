using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class InstanceTypeModel : BaseModel
    {
        private string _id;
        private int _virtualCpus;
        private long _memoryMib;
        private long _storageGb;
        private List<string> _architectures;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int VirtualCpus
        {
            get => _virtualCpus;
            set => SetProperty(ref _virtualCpus, value);
        }

        public long MemoryMib
        {
            get => _memoryMib;
            set => SetProperty(ref _memoryMib, value);
        }

        public long StorageGb
        {
            get => _storageGb;
            set => SetProperty(ref _storageGb, value);
        }

        public List<string> Architectures
        {
            get => _architectures;
            set => SetProperty(ref _architectures, value);
        }
    }
}
