using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Amazon.AWSToolkit;

using log4net;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2
{
    /// <summary>
    /// Class wrapping non-queryable metadata about the EC2 service resources
    /// </summary>
    public class EC2ServiceMeta
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(EC2ServiceMeta));
        readonly Dictionary<string, InstanceType> _instanceTypeMetaDictionary = new Dictionary<string, InstanceType>();
        IList<InstanceType> _allTypes = null;

        static readonly object _syncLock = new object();
        static EC2ServiceMeta _instance;
        public static EC2ServiceMeta Instance
        {
            get
            {
                EC2ServiceMeta meta;
                lock (_syncLock)
                {
                    meta = _instance;
                    if (meta == null)
                    {
                        try
                        {
                            meta = new EC2ServiceMeta();
                            meta.LoadFrom(@"ServiceMeta\EC2ServiceMeta.xml");
                            _instance = meta;
                        }
                        catch (Exception e)
                        {
                            // if we fail to load/parse the file, back an empty instance so caller can proceed, 
                            // but don't set instance so we try again
                            LOGGER.ErrorFormat("Failure during load/parse of EC2ServiceMeta - {0}", e.Message);
                            meta = new EC2ServiceMeta();
                        }
                    }
                }

                return meta;
            }
        }

        public IList<InstanceType> ALL
        {
            get
            {
                lock (_syncLock)
                {
                    if (_allTypes == null)
                    {
                        _allTypes = new List<InstanceType>();
                        foreach (string key in _instanceTypeMetaDictionary.Keys)
                        {
                            _allTypes.Add(_instanceTypeMetaDictionary[key]);
                        }
                    }
                }
                return _allTypes;
            }
        }

        public string DefaultInstanceTypeId
        {
            get;
            private set;
        }

        public InstanceType DefaultInstanceType
        {
            get;
            private set;
        }

        public static IList<InstanceType> GetValidTypes(Image image)
        {
            var metaTypes = EC2ServiceMeta.Instance.ALL;

            return metaTypes.Where(type => type.CanLaunch(image)).ToList();
        }

        public static InstanceType FindById(string id)
        {
            var types = EC2ServiceMeta.Instance.ALL;
            return types.FirstOrDefault(type => string.Equals(type.Id, id));
        }

        public InstanceType ById(string id)
        {
            var types = ALL;
            return types.FirstOrDefault(type => string.Equals(type.Id, id));
        }

        private void LoadFrom(string metadataFile)
        {
            var ec2MetaContent = S3FileFetcher.Instance.GetFileContent(metadataFile);
            if (string.IsNullOrEmpty(ec2MetaContent))
                throw new Exception("Could not fetch content to parse");

            try
            {
                XDocument xdoc = XDocument.Parse(ec2MetaContent);

                LoadInstanceTypeMetadata(xdoc);
            }
            catch (Exception e)
            {
                throw new Exception("Exception parsing EC2 meta content", e);
            }
        }

        private void LoadInstanceTypeMetadata(XDocument xdoc)
        {
            // do NOT call into static members here or anything that will cause a call to
            // the Instance member as we have not finished construction
            var instanceTypes
                = from p in xdoc.Root.Elements("InstanceTypes").Elements("InstanceType")
                    select new InstanceType(p.Attribute("id").Value,
                                            p.Element("DisplayName").Value,
                                            p.Element("Memory").Value,
                                            p.Element("DiskSpace").Value,
                                            Convert.ToInt32(p.Element("VirtualCores").Value),
                                            p.Element("ArchitectureBits").Value,
                                            Convert.ToBoolean(p.Element("RequiresEBSVolume").Value),
                                            Convert.ToBoolean(p.Element("RequiresHvmImage").Value),
                                            Convert.ToInt32(p.Element("MaxInstanceStore").Value),
                                            Convert.ToBoolean(p.Element("CurrentGeneration").Value),
                                            p.Element("HardwareFamily").Value)
                    {
                        RequiresVPC = p.Element("RequiresVPC") == null ? false : Convert.ToBoolean(p.Element("RequiresVPC").Value)
                    };

            foreach (var instanceType in instanceTypes)
            {
                if (!_instanceTypeMetaDictionary.ContainsKey(instanceType.Id))
                    _instanceTypeMetaDictionary.Add(instanceType.Id, instanceType);
                else
                {
                    LOGGER.InfoFormat("Found duplicate EC2 instance type meta for type {0}, using later version", instanceType.Id);
                    _instanceTypeMetaDictionary[instanceType.Id] = instanceType;
                }
            }

            string defaultInstanceTypeId;
            XElement xel = xdoc.Root.Element("DefaultInstanceType");
            if (xel != null)
                defaultInstanceTypeId = xel.Attribute("id").Value;
            else
                defaultInstanceTypeId = "m1.small";

            if (_instanceTypeMetaDictionary.ContainsKey(defaultInstanceTypeId))
            {
                DefaultInstanceTypeId = defaultInstanceTypeId;
                DefaultInstanceType = _instanceTypeMetaDictionary[defaultInstanceTypeId];
            }
        }
    }
}
