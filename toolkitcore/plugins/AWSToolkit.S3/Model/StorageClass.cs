using System.Collections.Generic;
using System.Linq;
using Amazon.S3;

namespace Amazon.AWSToolkit.S3.Model
{
    public class StorageClass
    {
        public static readonly StorageClass[] StorageClasses =
        {
            new StorageClass {S3StorageClass = S3StorageClass.Standard, Name = "Standard", IsGlacierClass = false},
            new StorageClass
            {
                S3StorageClass = S3StorageClass.ReducedRedundancy, Name = "Reduced Redundancy", IsGlacierClass = false
            },
            new StorageClass
            {
                S3StorageClass = S3StorageClass.IntelligentTiering, Name = "Intelligent Tiering", IsGlacierClass = false
            },
            new StorageClass
            {
                S3StorageClass = S3StorageClass.StandardInfrequentAccess, Name = "Standard-IA", IsGlacierClass = false
            },
            new StorageClass
                {S3StorageClass = S3StorageClass.OneZoneInfrequentAccess, Name = "One Zone-IA", IsGlacierClass = false},
            new StorageClass {S3StorageClass = S3StorageClass.Glacier, Name = "Glacier", IsGlacierClass = true}
            // TODO : GLACIER DEEP ARCHIVE (newer S3 SDK)
        };

        public static readonly IEnumerable<StorageClass> GlacierS3StorageClasses = StorageClasses
            .Where(sc => sc.IsGlacierClass);

        public static readonly IEnumerable<StorageClass> NonGlacierS3StorageClasses = StorageClasses
            .Where(sc => !sc.IsGlacierClass);

        public S3StorageClass S3StorageClass { get; set; }
        public string Name { get; set; }
        public bool IsGlacierClass { get; set; }
    }

    public static class StorageClassExtensionMethods
    {
        public static HashSet<S3StorageClass> AsS3StorageClassSet(this IEnumerable<StorageClass> storageClasses)
        {
            return new HashSet<S3StorageClass>(storageClasses.Select(sc => sc.S3StorageClass));
        }

        public static bool Contains(this IEnumerable<StorageClass> storageClasses, S3StorageClass storageClass)
        {
            return storageClasses.AsS3StorageClassSet().Contains(storageClass);
        }
    }
}