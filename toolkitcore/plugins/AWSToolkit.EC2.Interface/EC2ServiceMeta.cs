using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

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

        // Fallback values if we can't find a default size (as 'totalImageSize' in the quicklaunch 
        // image file. Some Windows images need 50GB min, so upsize our default.
        const int LinuxRootVolumeSizeFallback = 15;
        const int WindowsRootVolumeSizeFallback = 50;

        // just in case these could ever differ...
        const int WindowsAdditionalVolumeSizeFallback = 8;
        const int LinuxAdditionalVolumeSizeFallback = 8;

        const int MinIopsFallback = 100;
        const int MaxIopsFallback = 4000;

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

        public int DefaultWindowsRootVolumeSize
        {
            get;
            private set;
        }

        public int DefaultWindowsAdditionalVolumeSize
        {
            get;
            private set;
        }

        public int DefaultLinuxRootVolumeSize
        {
            get;
            private set;
        }

        public int DefaultLinuxAdditionalVolumeSize
        {
            get;
            private set;
        }

        public int MinIops
        {
            get;
            private set;
        }

        public int MaxIops
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

            var defaultInstanceTypeId = QuerySetting(xdoc, "DefaultInstanceType", "id", "m1.small");
            if (_instanceTypeMetaDictionary.ContainsKey(defaultInstanceTypeId))
            {
                DefaultInstanceTypeId = defaultInstanceTypeId;
                DefaultInstanceType = _instanceTypeMetaDictionary[defaultInstanceTypeId];
            }

            DefaultWindowsRootVolumeSize = QuerySetting(xdoc, "DefaultVolumeSizes/WindowsRootVolumeSize", null, WindowsRootVolumeSizeFallback);
            DefaultWindowsAdditionalVolumeSize = QuerySetting(xdoc, "DefaultVolumeSizes/WindowsAdditionalVolumeSize", null, WindowsAdditionalVolumeSizeFallback);

            DefaultLinuxRootVolumeSize = QuerySetting(xdoc, "DefaultVolumeSizes/LinuxRootVolumeSize", null, LinuxRootVolumeSizeFallback);
            DefaultLinuxAdditionalVolumeSize = QuerySetting(xdoc, "DefaultVolumeSizes/LinuxAdditionalVolumeSize", null, LinuxAdditionalVolumeSizeFallback);

            MinIops = QuerySetting(xdoc, "Iops", "min", MinIopsFallback);
            MaxIops = QuerySetting(xdoc, "Iops", "max", MaxIopsFallback);
        }

        int QuerySetting(XDocument xdoc, string elementName, string attributeName, int fallbackValue)
        {
            // pass empty string to avoid ToString/TryParse roundtrip on fallback value
            var val = QuerySetting(xdoc, elementName, attributeName, string.Empty);
            if (!string.IsNullOrEmpty(val))
            {
                int v;
                if (int.TryParse(val, out v))
                    return v;
            }

            return fallbackValue;
        }

        string QuerySetting(XDocument xdoc, string elementName, string attributeName, string fallbackValue)
        {
            var x = xdoc.Root.XPathSelectElement(elementName);
            if (x != null)
            {
                string val = null;
                if (!string.IsNullOrEmpty(attributeName))
                {
                    var attr = x.Attribute(attributeName);
                    if (attr != null)
                        val = attr.Value;
                }
                else
                    val = x.Value;

                if (!string.IsNullOrEmpty(val))
                    return val;
            }

            return fallbackValue;
        }

    }
}
