using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

using Amazon.AWSToolkit.CloudFormation.Parser;

namespace Amazon.AWSToolkit.CloudFormation.MSBuildTasks
{
    public class TemplateCompileTask : Task
    {

        private string[] sourceFiles;
        /// <summary>
        /// List of Python source files that should be compiled into the assembly
        /// </summary>
        [Required()]
        public string[] SourceFiles
        {
            get => sourceFiles;
            set => sourceFiles = value;
        }

        private string projectPath = null;
        /// <summary>
        /// This should be set to $(MSBuildProjectDirectory)
        /// </summary>
        [Required()]
        public string ProjectPath
        {
            get => projectPath;
            set => projectPath = value;
        }

        public override bool Execute()
        {
            int errorCount = 0;
            foreach (var sourceFile in this.SourceFiles)
            {
                errorCount += Compile(sourceFile);
            }

            return errorCount == 0;
        }

        private int Compile(string sourceFile)
        {
            int errorCount = 0;
            this.Log.LogMessage("Validating {0}", sourceFile);

            string filePath = Path.Combine(this.ProjectPath, sourceFile);
            string fileContents = File.ReadAllText(filePath);

            var lineMap = CreateLineMap(fileContents);

            errorCount += JsonValidation(filePath, fileContents);
            errorCount += ParserErrors(filePath, fileContents, lineMap);

            return errorCount;
        }

        int JsonValidation(string filePath, string fileContents)
        {
            int errorCount = 0;
            var jsonErrorToken = JSONValidatorWrapper.Validate(fileContents);
            if (jsonErrorToken != null)
            {
                this.Log.LogError(string.Empty, string.Empty, string.Empty, filePath, jsonErrorToken.LineNumber + 1, jsonErrorToken.LineStartOffset, jsonErrorToken.LineNumber + 1, jsonErrorToken.LineEndOffSet, jsonErrorToken.Message);
                errorCount++;
            }

            return errorCount;
        }

        int ParserErrors(string filePath, string fileContents, Tuple<int, int, int>[] lineMap)
        {
            int errorCount = 0;

            TemplateParser parser = new TemplateParser();
            ParserResults parserResults = parser.Parse(fileContents);

            foreach (var token in parserResults.HighlightedTemplateTokens)
            {
                switch (token.Type)
                {
                    case TemplateTokenType.InvalidKey:
                    case TemplateTokenType.DuplicateKey:
                    case TemplateTokenType.InvalidTypeReference:
                    case TemplateTokenType.NotAllowedValue:
                    case TemplateTokenType.UnknownMapKey:
                    case TemplateTokenType.UnknownReference:
                    case TemplateTokenType.UnknownResource:
                    case TemplateTokenType.UnknownResourceAttribute:

                        int lineStartNumber, lineStartOffeset;
                        FindPosition(lineMap, token.Postion, out lineStartNumber, out lineStartOffeset);
                        if (lineStartNumber == -1)
                            continue;

                        int lineEndNumber, lineEndOffeset;
                        FindPosition(lineMap, token.Postion + token.Length, out lineEndNumber, out lineEndOffeset);
                        if (lineEndNumber == -1)
                        {
                            lineEndNumber = lineStartNumber;
                            lineEndOffeset = lineStartOffeset;
                        }

                        this.Log.LogError(string.Empty, string.Empty, string.Empty, filePath, lineStartNumber + 1, lineStartOffeset, lineEndNumber + 1, lineEndOffeset, token.Decription);
                        break;
                    default:
                        continue;
                }
            }


            return errorCount;
        }

        void FindPosition(Tuple<int, int, int>[] lineMap, int position, out int lineNumber, out int lineOffset)
        {
            Func<int, int, Tuple<int, int, int>> binarySearch = null;
            binarySearch = (lower, upper) => 
                {
                    int interesection = (upper - lower) / 2 + lower;
                    var line = lineMap[interesection];

                    if (line.Item2 <= position && position < line.Item3)
                        return line;

                    if (lower == upper)
                        return null;

                    if (position < line.Item2)
                        upper = interesection;
                    else
                        lower = interesection;

                    return binarySearch(lower, upper);
                };

            var foundLine = binarySearch(0, lineMap.Length - 1);
            if (foundLine == null)
            {
                lineNumber = -1;
                lineOffset = -1;
                return;
            }

            lineNumber = foundLine.Item1;
            lineOffset = position - foundLine.Item2 - 1;
        }

        Tuple<int, int, int>[] CreateLineMap(string fileContents)
        {
            var lineMap = new List<Tuple<int, int, int>>();

            int lineNumber = 0;            
            int startPosition = 0;
            do
            {
                int endPostion = fileContents.IndexOf('\n', startPosition + 1);
                if(endPostion == -1)
                    lineMap.Add(new Tuple<int, int, int>(lineNumber, startPosition, fileContents.Length));
                else
                    lineMap.Add(new Tuple<int, int, int>(lineNumber, startPosition, endPostion));

                lineNumber++;
                startPosition = endPostion;
            } while (startPosition != -1);

            return lineMap.ToArray<Tuple<int, int, int>>();
        }
    }
}
