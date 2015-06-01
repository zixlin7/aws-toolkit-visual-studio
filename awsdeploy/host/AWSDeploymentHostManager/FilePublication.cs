using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public class FilePublication
    {
        public const string
            JSON_KEY_FULLPATH = "fullpath",
            JSON_KEY_S3NAME = "s3name",
            JSON_KEY_FILENAME = "filename",
            JSON_KEY_PATH = "path",
            JSON_KEY_SIZE = "size";

        public const string
            STATUS_PENDING = "pending",
            STATUS_COMPLETE = "complete",
            STATUS_IN_PROGRESS = "in_progress";

        private EntityObject pub;

        public FilePublication(string s3Name, string filename, string path, long size)
        {
            pub = new EntityObject(EntityType.FilePublication);
            S3Name = s3Name;
            FullPath = filename;
            FileName = filename.Substring(filename.LastIndexOf('\\') + 1);
            Path = path;
            Size = size;
            pub.Status = STATUS_PENDING;
        }

        public FilePublication(EntityObject eo)
        {
            pub = eo;
        }

        public void Persist()
        {
            PersistenceManager pm = new PersistenceManager();
            pm.Persist(pub);
        }

        public void WriteToJson(JsonWriter json)
        {
            json.WriteObjectStart();

            json.WritePropertyName(JSON_KEY_FILENAME);
            json.Write(S3Name);
            json.WritePropertyName(JSON_KEY_PATH);
            json.Write(Path);
            json.WritePropertyName(JSON_KEY_SIZE);
            json.Write(Size);

            json.WriteObjectEnd();
        }

        public static IList<FilePublication> LoadPending()
        {
            PersistenceManager pm = new PersistenceManager();
            IList<FilePublication> pubs = new List<FilePublication>();

            foreach (EntityObject eo in pm.SelectByStatus(EntityType.FilePublication, STATUS_PENDING))
            {
                pubs.Add(new FilePublication(eo));
            }
            return pubs;
        }

        public static FilePublication LoadPendingForFile(string filename)
        {
            HostManager.LOGGER.Debug(String.Format("Looking for filename: {0}", filename));
            foreach (FilePublication pub in LoadPending())
            {
                if (pub.FileName == null)
                {
                    pub.FileName = pub.FullPath.Substring(pub.FullPath.LastIndexOf('\\') + 1);
                }

                HostManager.LOGGER.Debug(String.Format("Checking filename: {0}", pub.FileName));
                if (pub.FileName.Equals(filename))
                {
                    HostManager.LOGGER.Debug(String.Format("Found filename: {0}", pub.FileName));
                    return pub;
                }
            }
            return null;
        }
        public static FilePublication LoadPendingForS3Name(string keyname)
        {
            foreach (FilePublication pub in LoadPending())
            {
                if (pub.S3Name.Equals(keyname))
                    return pub;
            }
            return null;
        }

        public void SetInProcess()
        {
            pub.Status = STATUS_IN_PROGRESS;
            Persist();
        }

        public void SetComplete()
        {
            pub.Status = STATUS_COMPLETE;
            Persist();
        }

        public void SetPending()
        {
            pub.Status = STATUS_PENDING;
            Persist();
        }

        public string S3Name
        {
            get
            {
                string filename = null;
                pub.Parameters.TryGetValue(JSON_KEY_S3NAME, out filename);
                return filename;
            }
            set
            {
                pub.Parameters[JSON_KEY_S3NAME] = value;
            }
        }

        public string FullPath
        {
            get
            {
                string filename = null;
                pub.Parameters.TryGetValue(JSON_KEY_FULLPATH, out filename);
                return filename;
            }
            set
            {
                pub.Parameters[JSON_KEY_FULLPATH] = value;
            }
        }

        public string FileName
        {
            get
            {
                string filename = null;
                pub.Parameters.TryGetValue(JSON_KEY_FILENAME, out filename);
                return filename;
            }
            set
            {
                pub.Parameters[JSON_KEY_FILENAME] = value;
            }
        }

        public string Path
        {
            get
            {
                string path = null;
                pub.Parameters.TryGetValue(JSON_KEY_PATH, out path);
                return path;
            }
            set 
            {
                pub.Parameters[JSON_KEY_PATH] = value;
            }
        }

        public long Size
        {
            get
            {
                long size = 0;
                string s = null;
                pub.Parameters.TryGetValue(JSON_KEY_SIZE, out s);
                if (s != null && s.Length > 0)
                {
                    try
                    {
                        size = Convert.ToInt64(s);
                    }
                    catch (FormatException) { }
                }
                return size;
            }
            set
            {
                pub.Parameters[JSON_KEY_SIZE] = value.ToString();
            }
        }
    }
}
