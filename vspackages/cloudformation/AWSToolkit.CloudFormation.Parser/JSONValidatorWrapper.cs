using System;

//using Newtonsoft.Json.Linq;


namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public static class JSONValidatorWrapper
    {

        public static ErrorToken Validate(string jsonDocument)
        {
            int lineNumber = 0;
            try
            {
                char? startingQuote = null;
                bool inEscapeMode = false;

                JsonCheckerTool.JsonChecker checker = new JsonCheckerTool.JsonChecker();
                for (int i = 0; i < jsonDocument.Length; i++)
                {
                    char current = jsonDocument[i];
                    bool onEscapeChar = false;

                    if (current == '\n')
                        lineNumber++;

                    // If we are in a string and not currently behind an escaping character and the current character is escaping character then mark it so.
                    if (startingQuote.HasValue && !inEscapeMode && current == '\\')
                    {
                        onEscapeChar = true;
                        inEscapeMode = true;
                    }
                    else
                    {
                        // If we are not inside a quoted string or 
                        // have hit the end of the line which quoted strings can't continue over then
                        // perform a validation
                        if (!startingQuote.HasValue || current == '\n')
                        {
                            checker.Check(current);
                        }

                        // If we are in a string then stop validation.
                        if ((current == '\"' || current == '\'') && !inEscapeMode)
                        {
                            // Starting a quoting string
                            if (!startingQuote.HasValue)
                            {
                                startingQuote = current;
                            }
                            // If the current quote is the same type as the starting quote
                            // then we found the end of the quoted string.
                            else if (startingQuote.HasValue && startingQuote.Value == current)
                            {
                                checker.Check(current);
                                startingQuote = null;
                            }
                        }
                    }

                    if (!onEscapeChar && inEscapeMode)
                        inEscapeMode = false;
                }
                    
                checker.FinalCheck();

                return null;
            }
            catch (Exception)
            {
                return new ErrorToken(ErrorTokenType.JSONLintvalidation, string.Format("Error: Parse error on line {0}", lineNumber + 1), true, lineNumber, 0, 0);
            }
        }

    }
}
