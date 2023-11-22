*******************************************************************************************************
* WARNING DO NOT USE THIS FILE AS-IS, IT REQUIRES REWORK AND COMMENTS WITH "LINE X" NEED TO BE FIXED. * 
*******************************************************************************************************

/////// Task 1: generate code suggestions as you type
// TODO: place your cursor at the end of line X and press Enter to generate a suggestion.

using System;
using System.Collections.Generic;

public class Program
{
    public static void Main()
    {
        List<Dictionary<string, string>> fakeUsers = new List<Dictionary<string, string>>();

        Dictionary<string, string> user1 = new Dictionary<string, string>();
        user1.Add("name", "User 1");
        user1.Add("id", "user1");
        user1.Add("city", "San Francisco");
        user1.Add("state", "CA");

        // Tip: press tab to accept the suggestion.
        fakeUsers.Add(user1);
    }
}

/////// Task 2: invoke CodeWhisperer manually
// TODO: Press Alt + C on line X to trigger CodeWhisperer

public class S3Uploader
{
    // Function to upload a file to an S3 bucket.
    public static void UploadFile(string filePath, string bucketName)
    {
        
    }
}
