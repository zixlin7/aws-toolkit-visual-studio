using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public class ApplicationVersion : Version
    {
        public ApplicationVersion(string url)
        {
            vers = new EntityObject(EntityType.ApplicationVersion);
            Url = url;
            ParseUrl();
        }

        public ApplicationVersion(string s3bucket, string s3Key)
        {
            vers = new EntityObject(EntityType.ApplicationVersion);
            S3Bucket = s3bucket;
            S3Key = s3Key;
        }

        public ApplicationVersion(EntityObject eo)
        {
            if (eo.EntityType != EntityType.ApplicationVersion)
                throw new ArgumentException("Attempted to create an ApplicationVersion with an EntiyObject of a different type.");

            vers = eo;
        }

        public string VersionLabel
        {
            get
            {
                int start = S3Key.IndexOf('/', 1) + 1;
                int end = S3Key.IndexOf('/', start);
                if (end == -1)
                    return "Unversioned";

                return S3Key.Substring(start, end - start);
            }
        }

        public void Persist()
        {
            PersistenceManager pm = new PersistenceManager();
            pm.Persist(vers);
        }

        public static ApplicationVersion LoadLatestVersion()
        {
            PersistenceManager pm = new PersistenceManager();
            EntityObject eo = pm.LoadLatest(EntityType.ApplicationVersion);
            if (eo != null)
                return new ApplicationVersion(eo);

            return null;
        }
    }
}
