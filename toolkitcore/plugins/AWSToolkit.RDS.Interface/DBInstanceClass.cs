namespace Amazon.AWSToolkit.RDS
{
    public class DBInstanceClass
    {
        /// <summary>
        /// The name of this instance type.
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// The RDS ID for this instance type.
        /// </summary>
        public string Id
        {
            get;
        }

        public string DisplayName => string.Format("{0} ({1})", Name, Id);

        /// <summary>
        /// The RAM (measured in Gigabytes) available on this instance type.
        /// </summary>
        public string MemoryWithUnits
        {
            get;
        }

        /// <summary>
        /// The number of virtual cores available on this instance type.
        /// </summary>
        public double NumberOfVirtualCores
        {
            get;
        }

        /// <summary>
        /// The architecture bits (32bit or 64bit) on this instance type.
        /// </summary>
        public string ArchitectureBits
        {
            get;
        }

        /// <summary>
        /// The I/O capacity (moderate, high etc) on this instance type
        /// </summary>
        public string IOCapacity
        {
            get;
        }

        public DBInstanceClass(string id, string name, string memoryInGigabytes,
                               double numberOfVirtualCores, string architectureBits, 
                               string ioCapacity)
        {
            this.Id = id;
            this.Name = name;
            this.MemoryWithUnits = memoryInGigabytes;
            this.NumberOfVirtualCores = numberOfVirtualCores;
            this.ArchitectureBits = architectureBits;
            this.IOCapacity = ioCapacity;
        }
    }
}
