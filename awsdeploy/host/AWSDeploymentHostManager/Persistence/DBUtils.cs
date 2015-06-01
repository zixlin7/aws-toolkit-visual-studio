using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Data;
using System.Data.SQLite;

using ThirdParty.Json.LitJson;

using log4net;

namespace AWSDeploymentHostManager.Persistence
{
    internal static class DBUtils
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(DBUtils));

        internal static void MakeSureDBExists(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                LOGGER.DebugFormat("Creating database: {0}", fullPath);
                try
                {
                    SQLiteConnection.CreateFile(fullPath);
                }
                catch (Exception e)
                {
                    LOGGER.Debug(string.Format("Error creating database: {0}", fullPath), e);
                }
            }
        }

        internal static void DeleteDatabase(string fullPath)
        {
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(i * 100);

                try
                {
                    File.Delete(fullPath);
                    break;
                }
                catch (IOException ioe)
                {
                    LOGGER.Info(string.Format("Error deleteing database: {0}", fullPath), ioe);

                    if (i < 4)
                    {
                        LOGGER.Info("Retrying delete");
                    }
                    else
                    {
                        LOGGER.Error("Too many failures, abandoning delete attempt");
                    }
                }
            }
                
        }

        internal static void CreateEntityTable(SQLiteConnection connection, EntityType entityType)
        {
            string script =
                "CREATE TABLE IF NOT EXISTS " + entityType.ToString() + " " +
                "(" +
                "   id TEXT PRIMARY KEY," +
                "   timestamp TEXT," +
                "   status TEXT," +
                "   parameters TEXT" +
                ")";

            SQLiteCommand command = new SQLiteCommand(script, connection);
            command.ExecuteNonQuery();
        }

        internal static void Insert(SQLiteConnection connection, EntityObject entity)
        {
            var script = string.Format("INSERT INTO {0} (id, timestamp, status, parameters) VALUES(@Id, @Timestamp, @Status, @Parameters)", entity.EntityType.ToString());
            var command = new SQLiteCommand(script, connection);

            Guid id = Guid.NewGuid();
            DateTime timestamp = DateTime.Now;
            string json = convertParameters(entity.Parameters);

            command.Parameters.AddWithValue("@Id", id.ToString());
            command.Parameters.AddWithValue("@Timestamp", formatDateTime(timestamp));
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@Parameters", json);

            command.ExecuteNonQuery();

            entity.Id = id;
            entity.Timestamp = DateTime.Parse(formatDateTime(timestamp));
        }

        internal static void Update(SQLiteConnection connection, EntityObject entity)
        {
            var script = string.Format("UPDATE {0} SET timestamp = @Timestamp, status = @Status, parameters = @Parameters WHERE id = @Id", entity.EntityType.ToString());
            var command = new SQLiteCommand(script, connection);

            string timestamp = formatDateTime(DateTime.Now);
            string json = convertParameters(entity.Parameters);

            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Timestamp", timestamp);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@Parameters", json);

            command.ExecuteNonQuery();

            entity.Timestamp = DateTime.Parse(timestamp);
        }

        internal static EntityObject LoadEntity(SQLiteConnection connection, EntityType entityType, Guid id)
        {
            var script = string.Format("SELECT id, timestamp, status, parameters FROM {0} WHERE id = @Id", entityType.ToString());
            var command = new SQLiteCommand(script, connection);
            command.Parameters.AddWithValue("Id", id.ToString());

            var list = select(connection, command, entityType);
            if (list.Count == 0)
                return null;
            return list[0];
        }

        internal static EntityObject LoadLatest(SQLiteConnection connection, EntityType entityType)
        {
            var script = string.Format("SELECT id, timestamp, status, parameters FROM {0} order by timestamp desc limit 1", entityType.ToString());
            var command = new SQLiteCommand(script, connection);

            var list = select(connection, command, entityType);
            if (list.Count == 0)
                return null;
            return list[0];
        }

        internal static IList<EntityObject> SelectByStatus(SQLiteConnection connection, EntityType entityType, string status)
        {
            var script = string.Format("SELECT id, timestamp, status, parameters FROM {0} WHERE status = @Status", entityType.ToString());
            var command = new SQLiteCommand(script, connection);
            command.Parameters.AddWithValue("Status", status);
            return select(connection, command, entityType);
        }

        internal static IList<EntityObject> SelectByGreaterThanTimestamp(SQLiteConnection connection, EntityType entityType, DateTime timestamp)
        {
            var script = string.Format("SELECT id, timestamp, status, parameters FROM {0} WHERE timestamp > @Timestamp", entityType.ToString());
            var command = new SQLiteCommand(script, connection);
            command.Parameters.AddWithValue("Timestamp", formatDateTime(timestamp));
            return select(connection, command, entityType);
        }

        internal static IList<EntityObject> SelectByTimestampRange(SQLiteConnection connection, EntityType entityType, DateTime startTime, DateTime endTime)
        {
            var script = string.Format("SELECT id, timestamp, status, parameters FROM {0} WHERE timestamp >= @StartTime AND timestamp <= @EndTime", entityType.ToString());
            var command = new SQLiteCommand(script, connection);
            command.Parameters.AddWithValue("StartTime", formatDateTime(startTime));
            command.Parameters.AddWithValue("EndTime", formatDateTime(endTime));

            return select(connection, command, entityType);
        }

        static IList<EntityObject> select(SQLiteConnection connection, SQLiteCommand command, EntityType entityType)
        {
            List<EntityObject> list = new List<EntityObject>();
            using (SQLiteDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    list.Add(createEntityObject(reader, entityType));
                }
            }

            return list;
        }

        static EntityObject createEntityObject(SQLiteDataReader reader, EntityType entityType)
        {
            EntityObject eo = new EntityObject(entityType);
            eo.Id = Guid.Parse(Convert.ToString(reader["id"]));
            eo.Timestamp = DateTime.Parse(Convert.ToString(reader["timestamp"]), CultureInfo.InvariantCulture).ToLocalTime();
            eo.Status = Convert.ToString(reader["status"]);
            eo.Parameters = parseParameters(Convert.ToString(reader["parameters"]));

            return eo;
        }


        static Dictionary<string, string> parseParameters(string json)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                JsonData data = JsonMapper.ToObject(json);

                foreach (KeyValuePair<string, JsonData> kvp in data)
                {
                    if (kvp.Value.IsString)
                        parameters[kvp.Key] = (string)kvp.Value;
                    else
                        parameters[kvp.Key] = kvp.Value.ToJson();
                }
            }

            return parameters;
        }

        static string convertParameters(IDictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.WriteObjectStart();
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                writer.WritePropertyName(kvp.Key);
                writer.Write(kvp.Value);
            }
            writer.WriteObjectEnd();

            return sb.ToString();
        }

        static string formatDateTime(DateTime date)
        {
            return date.ToUniversalTime().ToString(
                "yyyy-MM-dd\\THH:mm:ss.fff\\Z",
                CultureInfo.InvariantCulture
                );
        }
    }
}
