using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.DynamoDBv2.DataModel;

namespace $safeprojectname$
{
    public static class S3LinkOperations
    {
        public static void RunOperations(DynamoDBContext context)
        {
            Console.WriteLine();
            Console.WriteLine("Creating and saving S3Link");
            CreateProfileAndUploadImage(context);

            Console.WriteLine();
            Console.WriteLine("Loading profile and downloading image with S3Link");
            LoadProfileAndDownloadImage(context);
        }

        private static void CreateProfileAndUploadImage(DynamoDBContext context)
        {
            Console.WriteLine("Creating Jeff Bezos' Profile");
            Profile JeffBezos = new Profile()
            {
                Name = "Jeff Bezos",
                ProfilePicture = S3Link.Create(context, Program.BucketName, "JeffBezos.jpg", Amazon.RegionEndpoint.USEast1),
                Age = DateTime.Now.Year - 1964,
                Likes = new List<string>() { "Kindle Fire", "Fire Phone", "Amazon Prime" }
            };

            Console.WriteLine("Saving Jeff's profile to DynamoDB");
            context.Save<Profile>(JeffBezos);

            Console.WriteLine("Uploading his profile picture to S3");
            JeffBezos.ProfilePicture.UploadFrom("C:\\bezos.jpg");

            Console.WriteLine("Getting URL to image...");
            Console.WriteLine("URL is: {0}", JeffBezos.ProfilePicture.GetPreSignedURL(DateTime.Now.Add(TimeSpan.FromMinutes(5))));
        }

        private static void LoadProfileAndDownloadImage(DynamoDBContext context)
        {
            Console.WriteLine("Creating Jeff Bezos' Profile");
            Profile BezosLoaded = context.Load<Profile>("Jeff Bezos");

            BezosLoaded.ProfilePicture.DownloadTo("bezosDown.jpg");
        }
    }
}
