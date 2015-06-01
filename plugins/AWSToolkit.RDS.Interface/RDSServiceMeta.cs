using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Amazon.AWSToolkit;

using log4net;

namespace Amazon.AWSToolkit.RDS
{
    /// <summary>
    /// Class wrapping non-queryable metadata about the RDS service resources
    /// </summary>
    public class RDSServiceMeta
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RDSServiceMeta));
        Dictionary<string, DBEngineMeta> _engineMetaDictionary = new Dictionary<string, DBEngineMeta>();
        Dictionary<string, DBInstanceClass> _instanceClassDictionary = new Dictionary<string, DBInstanceClass>();
        IList<DBInstanceClass> _allClasses = null;

        static object _syncLock = new object();
        static RDSServiceMeta _instance;
        public static RDSServiceMeta Instance
        {
            get
            {
                RDSServiceMeta meta;
                lock (_syncLock)
                {
                    meta = _instance;
                    if (meta == null)
                    {
                        try
                        {
                            meta = new RDSServiceMeta();
                            meta.LoadFrom(@"ServiceMeta\RDSServiceMeta.xml");
                            _instance = meta;
                        }
                        catch (Exception e)
                        {
                            // if we fail to load/parse the file, back an empty instance so caller can proceed, 
                            // but don't set instance so we try again
                            LOGGER.ErrorFormat("Failure during load/parse of RDSServiceMeta - {0}", e.Message);
                            meta = new RDSServiceMeta();
                        }
                    }
                }

                return meta;
            }
        }

        public DBEngineMeta MetaForEngine(string dbEngineId)
        {
            // do an exact check first to allow for specificity if multiple same-vendor engines in future
            if (_engineMetaDictionary.ContainsKey(dbEngineId))
                return _engineMetaDictionary[dbEngineId];
            else
            {
                // now do a general vendor comparison and return first hit
                foreach (string key in _engineMetaDictionary.Keys)
                {
                    if (_engineMetaDictionary[key].DBEngine.StartsWith(dbEngineId, StringComparison.InvariantCultureIgnoreCase))
                        return _engineMetaDictionary[key];
                }
            }

            LOGGER.WarnFormat("DB engine metadata set does not contain an entry for engine id {0}", dbEngineId);
            System.Diagnostics.Debug.Assert(false, "DB engine metadata set does not contain an entry for engine id " + dbEngineId);

            return null;
        }

        public DBInstanceClass MetaForDBInstanceClass(string dbInstanceClassId)
        {
            if (_instanceClassDictionary.ContainsKey(dbInstanceClassId))
                return _instanceClassDictionary[dbInstanceClassId];
            else
            {
                LOGGER.WarnFormat("DB Instance Class metadata set does not contain an entry for class id {0}", dbInstanceClassId);
                System.Diagnostics.Debug.Assert(false, "DB Instance Class metadata set does not contain an entry for class id " + dbInstanceClassId);
            }

            return null;
        }

        /// <summary>
        /// Convenience property to return all defined DB instance classes
        /// </summary>
        public static IEnumerable<DBInstanceClass> ALL
        {
            get
            {
                return RDSServiceMeta.Instance.MetaForDBInstancesClasses(null);
            }
        }

        /// <summary>
        /// Returns collection of instance class metadata matching the supplied instance class ids.
        /// If a null or empty list is supplied, all classes are returned.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IEnumerable<DBInstanceClass> MetaForDBInstancesClasses(IEnumerable<string> list)
        {
            if (list == null || list.Count<string>() == 0)
            {
                lock (_syncLock)
                {
                    if (_allClasses == null)
                    {
                        foreach (string key in _instanceClassDictionary.Keys)
                        {
                            _allClasses.Add(_instanceClassDictionary[key]);
                        }
                    }
                }
                return _allClasses;
            }
            else
            {
                List<DBInstanceClass> instanceClasses = new List<DBInstanceClass>();
                foreach (string key in list)
                {
                    if (_instanceClassDictionary.ContainsKey(key))
                        instanceClasses.Add(_instanceClassDictionary[key]);
                    else
                        LOGGER.WarnFormat("Unable to find entry for DB instance class id {0} in loaded metadata", key);
                }
                return instanceClasses;
            }
        }

        private void LoadFrom(string metadataFile)
        {
            string rdsMetaContent = S3FileFetcher.Instance.GetFileContent(metadataFile);
            if (string.IsNullOrEmpty(rdsMetaContent))
                throw new Exception("Could not fetch content to parse");

            try
            {
                XDocument xdoc = XDocument.Parse(rdsMetaContent);

                LoadInstanceClassMetadata(xdoc);
                LoadDBEngineMetadata(xdoc);
            }
            catch (Exception e)
            {
                throw new Exception("Exception parsing RDS meta content", e);
            }
        }

        private void LoadInstanceClassMetadata(XDocument xdoc)
        {
            // do NOT call into static members here or anything that will cause a call to
            // the Instance member as we have not finished construction
            var instanceClasses
                = from p in xdoc.Root.Elements("DBInstanceTypes").Elements("DBInstanceType")
                  select new DBInstanceClass(p.Attribute("id").Value,
                                             p.Element("DisplayName").Value,
                                             p.Element("Memory").Value,
                                             Convert.ToDouble(p.Element("VirtualCores").Value),
                                             p.Element("ArchitectureBits").Value,
                                             p.Element("IOCapacity").Value
                                            );

            foreach (var instanceClass in instanceClasses)
            {
                if (!_instanceClassDictionary.ContainsKey(instanceClass.Id))
                    _instanceClassDictionary.Add(instanceClass.Id, instanceClass);
                else
                {
                    LOGGER.InfoFormat("Found duplicate RDS instance class meta for class {0}, using later version", instanceClass.Id);
                    _instanceClassDictionary[instanceClass.Id] = instanceClass;
                }
            }
        }

        private void LoadDBEngineMetadata(XDocument xdoc)
        {
            // do NOT call into static members here or anything that will cause a call to
            // the Instance member as we have not finished construction
            var engines
                = from e in xdoc.Root.Elements("DBEngineMetas").Elements("DBEngineMeta")
                  select new DBEngineMeta(e.Attribute("id").Value,
                                          Convert.ToInt32(e.Element("DefaultPort").Value),
                                          Convert.ToInt32(e.Element("DefaultPort").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("DefaultPort").Attribute("max").Value),
                                          (
                                            from l in e.Elements("LicenseModels").Elements("LicenseModel")
                                                select l.Attribute("id").Value
                                          ),
                                          (
                                            from i in e.Elements("DBInstanceClasses").Elements("DBInstanceClass")
                                                select i.Attribute("id").Value
                                          ),
                                          Convert.ToBoolean(e.Element("SupportsMultiAZ").Value),
                                          Convert.ToInt32(e.Element("Storage").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("Storage").Attribute("max").Value),
                                          Convert.ToInt32(e.Element("DBIdentifierLength").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("DBIdentifierLength").Attribute("max").Value),
                                          Convert.ToInt32(e.Element("DBNameLength").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("DBNameLength").Attribute("max").Value),
                                          Convert.ToInt32(e.Element("ParamGroupNameLength").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("ParamGroupNameLength").Attribute("max").Value),
                                          Convert.ToInt32(e.Element("UserNameLength").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("UserNameLength").Attribute("max").Value),
                                          Convert.ToInt32(e.Element("PasswordLength").Attribute("min").Value),
                                          Convert.ToInt32(e.Element("PasswordLength").Attribute("max").Value)
                                         );

            foreach (var engine in engines)
            {
                if (!_engineMetaDictionary.ContainsKey(engine.DBEngine))
                    _engineMetaDictionary.Add(engine.DBEngine, engine);
                else
                {
                    LOGGER.InfoFormat("Found duplicate RDS engine meta for engine {0}, using later version", engine.DBEngine);
                    _engineMetaDictionary[engine.DBEngine] = engine;
                }
            }
        }

        private RDSServiceMeta() {}
    }
}
