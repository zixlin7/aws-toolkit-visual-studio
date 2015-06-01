using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Data;
using System.Data.SQLite;

using log4net;

namespace AWSDeploymentHostManager.Persistence
{
    public enum EntityType { Event, FilePublication, Metric, ApplicationVersion, ConfigVersion, TimeStamp };

    public class PersistenceManager
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(PersistenceManager));

        const string DEFAULT_DB_FILENAME = "..\\hostmanager.db";
        static object LOCK_OBJ = new object();

        static PersistenceManager()
        {
            LOGGER.Debug("PersistenceManager initialize");
            DBUtils.MakeSureDBExists(getFullPath());
        }

        public void Persist(EntityObject entity)
        {
            using (SQLiteConnection connection = openConnection(entity.EntityType))
            {
                if(entity.Id == Guid.Empty)
                    DBUtils.Insert(connection, entity);
                else
                    DBUtils.Update(connection, entity);
            }
        }

        public EntityObject Load(EntityType entityType, Guid id)
        {
            using (SQLiteConnection connection = openConnection(entityType))
            {
                EntityObject eo = DBUtils.LoadEntity(connection, entityType, id);
                return eo;
            }
        }

        public EntityObject LoadLatest(EntityType entityType)
        {
            using (SQLiteConnection connection = openConnection(entityType))
            {
                return DBUtils.LoadLatest(connection, entityType);
            }
        }

        public IList<EntityObject> SelectByStatus(EntityType entityType, string status)
        {
            using (SQLiteConnection connection = openConnection(entityType))
            {
                return DBUtils.SelectByStatus(connection, entityType, status);
            }
        }

        public IList<EntityObject> SelectByGreaterThanTimestamp(EntityType entityType, DateTime timestamp)
        {
            using (SQLiteConnection connection = openConnection(entityType))
            {
                return DBUtils.SelectByGreaterThanTimestamp(connection, entityType, timestamp);
            }
        }

        public IList<EntityObject> SelectByTimestampRange(EntityType entityType, DateTime startTime, DateTime endTime)
        {
            using (SQLiteConnection connection = openConnection(entityType))
            {
                return DBUtils.SelectByTimestampRange(connection, entityType, startTime, endTime);
            }
        }

        SQLiteConnection openConnection(EntityType entityType)
        {
            SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source=\"{0}\"", getFullPath()));
            conn.Open();
            makeSureTableExist(conn, entityType);
            return conn;
        }

        static HashSet<EntityType> _createdTables = new HashSet<EntityType>();
        static void makeSureTableExist(SQLiteConnection connection, EntityType entityType)
        {
            if (!_createdTables.Contains(entityType))
            {
                lock (LOCK_OBJ)
                {
                    DBUtils.CreateEntityTable(connection, entityType);
                    _createdTables.Add(entityType);
                }
            }
        }

        static string _fullPath;
        static string getFullPath()
        {
            if (_fullPath == null)
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                int pos = loc.LastIndexOf(@"\");
                _fullPath = loc.Substring(0, pos + 1) + DEFAULT_DB_FILENAME;
            }
            return _fullPath;
        }

        public static void ResetDatabaseForTesting()
        {
            _createdTables.Clear();
            DBUtils.DeleteDatabase(getFullPath());
            DBUtils.MakeSureDBExists(getFullPath());
        }
    }
}
