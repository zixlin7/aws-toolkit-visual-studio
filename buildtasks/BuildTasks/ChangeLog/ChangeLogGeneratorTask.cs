using System;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Build.Framework;
using System.Collections.Generic;

namespace BuildTasks.ChangeLog
{
    public class ChangeLogGeneratorTask : BuildTaskBase
    {
        /// <summary>
        /// The root folder of the repository containing change directory and files to be updated
        /// </summary>
        public string RepositoryRoot { get; set; }

        /// <summary>
        /// List of change types
        /// </summary>
        private static readonly List<string> AllChangeTypes = new List<string>
            {"Breaking Change", "Feature", "Bug Fix", "Deprecation", "Removal", "Test"};

        /// <summary>
        /// The next-release directory path containing changelogs queued up for a new release
        /// </summary>
        [Output]
        public string NextReleasePath { get; set; }

        public override bool Execute()
        {
            //generate new change log details and write to json file
            NextReleasePath = Path.Combine(RepositoryRoot, ".changes", "next-release");
            Console.WriteLine("You are generating a new changelog");
            var changeType = PromptChangeType();
            //check if user chose cancel:null
            if (string.IsNullOrEmpty(changeType))
            {
                return false;
            }

            var message = PromptChangeMessage();
            WriteChangeToFile(changeType, message, NextReleasePath);
            return true;
        }

        /// <summary>
        /// Method to prompt users to choose a change type
        /// </summary>
        /// <returns>change type</returns>
        public static string PromptChangeType()
        {
            while (true)
            {
                for (var i = 0; i < AllChangeTypes.Count; i++)
                {
                    Console.WriteLine("[{0}] {1}", i + 1, AllChangeTypes[i]);
                }

                Console.WriteLine("[{0}] Cancel{1}", 0, Environment.NewLine);
                Console.Write($"Please enter type of change[0..{AllChangeTypes.Count}]:");

                //check if user input for type is valid
                var validInputFlag = int.TryParse(Console.ReadLine(), out var userInput);
                if (!validInputFlag || userInput < 0 || userInput > (AllChangeTypes.Count))
                {
                    Console.WriteLine($"Invalid change type, change type must be between 0 and {AllChangeTypes.Count}");
                    Console.WriteLine();
                }

                else if (userInput == 0)
                {
                    Console.WriteLine("Cancelling change");
                    return null;
                }
                else
                {
                    return AllChangeTypes[userInput - 1];
                }
            }
        }

        /// <summary>
        /// Prompt users to enter description for the change
        /// </summary>
        /// <returns>change message</returns>
        public static string PromptChangeMessage()
        {
            while (true)
            {
                Console.Write("Enter change message:");
                var messageText = Console.ReadLine();
                if (!string.IsNullOrEmpty(messageText))
                {
                    return messageText;
                }
            }
        }

        /// <summary>
        /// Creates a ChangeLogEntry object, converts it into json string and writes
        /// to a file in the next-release directory
        /// </summary>
        /// <param name="type"></param>
        /// <param name="changeMessage"></param>
        /// <param name="parentFolder"></param>
        public static void WriteChangeToFile(string type, string changeMessage, string parentFolder)
        {
            var changeObj = new ChangeLogEntry {Type = type, Description = changeMessage};
            var jsonString = JsonConvert.SerializeObject(changeObj, Formatting.Indented);

            Directory.CreateDirectory(parentFolder);
            var filename = $"{type}-{Guid.NewGuid()}.json";
            var changeFilePath = Path.Combine(parentFolder, filename);
            File.WriteAllText(changeFilePath, jsonString);
            Console.WriteLine($"Change log written to {changeFilePath}");
        }
    }
}