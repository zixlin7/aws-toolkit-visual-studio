using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class JsonDocument
    {
        int _position;
        string _document;
        StringReader _reader;
        IndexableStack<string> _keyChain = new IndexableStack<string>();
        Stack<Mutable<bool>> _addedKeys = new Stack<Mutable<bool>>();
        string _computedKeyChainString = null;
        bool _key = true;
        Stack<bool> _inArray = new Stack<bool>();
        JsonToken _current = new JsonToken();
        
        JsonToken _previous = new JsonToken();
        int _previousPosition;

        List<ErrorToken> _errorTokens = new List<ErrorToken>();


        public JsonDocument(string document)
        {
            this._document = document;
            this._reader = new StringReader(this._document);
            this._position = 0;
        }

        private int ReadStream()
        {
            int value = this._reader.Read();
            this._position++;
            return value;
        }

        public JsonToken CurrentToken => this._current;

        public List<ErrorToken> ErrorTokens => this._errorTokens;

        public string KeyChainString
        {
            get 
            {
                if (this._computedKeyChainString == null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in this._keyChain)
                        sb.AppendFormat("/{0}", item);

                    this._computedKeyChainString = sb.ToString();
                }

                return this._computedKeyChainString; 
            }
        }

        public IndexableStack<string> KeyChain => this._keyChain;

        public int Peek()
        {
            return this._reader.Peek();
        }

        public bool EndOfStream => this._document.Length <= this._position;

        private int ReadStream(char[] buffer, int index, int count)
        {
            var bytesRead = this._reader.Read(buffer, index, count);
            this._position += bytesRead;
            return bytesRead;
        }

        public int Position => this._position;

        public string OriginalDocument => this._document;

        public bool IsCurrentScalerValue =>
            this._current.Type == JsonTokenType.Text ||
            this._current.Type == JsonTokenType.Number ||
            this._current.Type == JsonTokenType.Boolean ||
            this._current.Type == JsonTokenType.Null;

        public bool ReadToNextKey()
        {
            while (Read())
            {
                switch (this._current.Type)
                {
                    case JsonTokenType.Text:
                        return true;
                    case JsonTokenType.KeyValueSeperator:
                        return false;
                    case JsonTokenType.EndElement:
                        return false;
                    case JsonTokenType.EndArray:
                        return false;
                }
            }

            return false;
        }


        public bool Read()
        {
            if (this.EndOfStream)
            {
                return false;
            }

            _previous = _current;
            this._previousPosition = this._position;
            _current = ReadToken();

            if (_current.Type == JsonTokenType.None && this.EndOfStream)
            {
                return false;
            }

            if (_current.Type != JsonTokenType.EndElement  && this._addedKeys.Count > 0)
            {
                this._addedKeys.Peek().Value = true;
            }

            switch (_current.Type)
            {
                case JsonTokenType.StartElement:
                    _inArray.Push(false);
                    _key = true;
                    this._addedKeys.Push(new Mutable<bool>(false));
                    break;
                case JsonTokenType.EndElement:
                    if (_inArray.Pop())
                    {
                        throw new ParseException("']' expected but '}' found.");
                    }
                    if (this._addedKeys.Peek().Value)
                    {
                        _keyChain.Pop();
                        _computedKeyChainString = null;
                    }
                    this._addedKeys.Pop();
                    break;
                case JsonTokenType.ElementSeperator:
                    if (_inArray.Peek())
                    {
                        _key = false;
                    }
                    else
                    {
                        _key = true;
                        _keyChain.Pop();
                        _computedKeyChainString = null;
                    }
                    break;
                case JsonTokenType.Text:
                    if (_key)
                    {
                        _keyChain.Push(_current.Text);
                        _computedKeyChainString = null;

                        //if (this._previous.Type != JsonTokenType.ElementSeperator &&
                        //        this._previous.Type != JsonTokenType.StartElement &&
                        //        this._previous.Type != JsonTokenType.StartArray)
                        //{
                        //    this._errorTokens.Add(new ErrorToken(ErrorTokenType.MissingElementSeparator, this._previousPosition, this._position));
                        //}
                    }
                    break;
                case JsonTokenType.KeyValueSeperator:
                    _key = false;
                    break;
                case JsonTokenType.StartArray:
                    _inArray.Push(true);
                    _key = false;
                    break;
                case JsonTokenType.EndArray:
                    if (!_inArray.Pop())
                    {
                        throw new ParseException("'}' expected but ']' found.");
                    }
                    break;
                case JsonTokenType.Number:
                case JsonTokenType.Boolean:
                case JsonTokenType.Null:
                    break;
                default:
                    throw new ParseException("Invalid json token");
            }
            return true;
        }


        public JsonToken ReadToken()
        {
            JsonToken ret = new JsonToken();
            int nextChar = this.ReadStream();

            if (nextChar == -1)
            {
                return ret;
            }

            while (Char.IsWhiteSpace((char)nextChar))
            {
                nextChar = this.ReadStream();
                if (this.EndOfStream)
                {
                    return ret;
                }

                if (nextChar == -1)
                {
                    return ret;
                }
            }

            ret.Position = this.Position - 1;
            switch (nextChar)
            {
                case '{':
                    ret.Type = JsonTokenType.StartElement;
                    break;
                case '}':
                    ret.Type = JsonTokenType.EndElement;
                    break;
                case ',':
                    ret.Type = JsonTokenType.ElementSeperator;
                    break;
                case '"':
                    ret.Type = JsonTokenType.Text;
                    ret.Text = ReadQuote();
                    ret.Length = (int)(this.Position - ret.Position);
                    break;
                case ':':
                    ret.Type = JsonTokenType.KeyValueSeperator;
                    break;
                case '[':
                    ret.Type = JsonTokenType.StartArray;
                    break;
                case ']':
                    ret.Type = JsonTokenType.EndArray;
                    break;
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    ret.Type = JsonTokenType.Number;
                    ret.Text = ReadNumber((char)nextChar);
                    ret.Length = (int)(this.Position - ret.Position);
                    break;
                case 't':
                case 'T':
                    ret.Type = JsonTokenType.Boolean;
                    if (!Read("rue",  StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new InvalidDataException("Invalid JSON data");
                    }
                    ret.Text = "true";
                    ret.Length = (int)(this.Position - ret.Position);
                    break;
                case 'f':
                case 'F':
                    ret.Type = JsonTokenType.Boolean;
                    if (!Read("alse", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new InvalidDataException("Invalid JSON data");
                    }
                    ret.Text = "false";
                    ret.Length = (int)(this.Position - ret.Position);
                    break;
                case 'n':
                case 'N':
                    ret.Type = JsonTokenType.Null;
                    if (!Read("ull", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new ParseException("Invalid JSON data");
                    }
                    ret.Text = null;
                    ret.Length = (int)(this.Position - ret.Position);
                    break;
                default:
                    throw new ParseException("Invalid JSON data");
            }
            return ret;
        }

        /// <summary>
        /// Reads a json quotation from the stream
        /// </summary>
        /// <returns>the string representation of the quotation</returns>
        private string ReadQuote()
        {
            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            int nextChar;

            while ((((nextChar = this.ReadStream()) != '"') || (escaped)) && (IsTokenChar(nextChar)))
            {
                if ((char) nextChar == '\\')
                {
                    if (escaped)
                    {
                        sb.Append((char) nextChar); // not calling GetEscapedChar, it would simply return \
                    }
                    escaped = !escaped;
                }
                else
                {
                    if (escaped)
                    {
                        char escapedChar = GetEscapedChar(nextChar);
                        sb.Append(escapedChar);
                    }
                    else
                    {
                        sb.Append((char) nextChar);
                    }
                    escaped = false;
                }
            }

            return sb.ToString();
        }

        public bool IsTokenChar(int nextChar)
        {
            return nextChar != -1 && !IsNewLine(nextChar);
        }

        public bool IsNewLine(int nextChar)
        {
            return nextChar == '\n' || nextChar == '\r';
        }

        private static char GetEscapedChar(int character)
        {
            switch (character)
            {
                case 'n':
                    return '\n';

                case 't':
                    return '\t';

                case 'r':
                    return '\r';

                case 'b':
                    return '\b';

                case 'f':
                    return '\f';
                default:
                    return Convert.ToChar(character);
            }
        }

        /// <summary>
        /// Reads a json number from the stream
        /// </summary>
        /// <param name="firstChar">first character of the numbers which has already been read</param>
        /// <returns>the string representation of the number</returns>
        private string ReadNumber(char firstChar)
        {
            StringBuilder sb = new StringBuilder(firstChar.ToString());
            char nextChar;

            while (Char.IsNumber((char)this.Peek()))
            {
                nextChar = (char)this.ReadStream();
                sb.Append(nextChar);
            }

            if (((char)this.Peek()) == '.')
            {
                nextChar = (char)this.ReadStream();
                sb.Append(nextChar);
                while (Char.IsNumber((char)this.Peek()))
                {
                    nextChar = (char)this.ReadStream();
                    sb.Append(nextChar);
                }
            }

            if ((((char)this.Peek()) == 'e') || (((char)this.Peek()) == 'E'))
            {
                nextChar = (char)this.ReadStream();
                sb.Append(nextChar);
                if ((((char)this.Peek()) == '+') || (((char)this.Peek()) == '-'))
                {
                    nextChar = (char)this.ReadStream();
                    sb.Append(nextChar);
                }
                while (Char.IsNumber((char)this.Peek()))
                {
                    nextChar = (char)this.ReadStream();
                    sb.Append(nextChar);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks that the stream contains toRead next when matched using comparisonType
        /// </summary>
        /// <param name="toRead">The string to verify is next in the stream</param>
        /// <param name="from">The stream to read from</param>
        /// <param name="comparisonType">The type of comparison to perform</param>
        /// <returns>If the stream started with toRead or not</returns>
        private bool Read(string toRead, StringComparison comparisonType)
        {
            bool ret = true;
            char[] value = new char[toRead.Length];
            this.ReadStream(value, 0, value.Length);

            if (!toRead.Equals(new string(value), comparisonType))
            {
                ret = false;
            }
            return ret;
        }

        public string PeekChildAttribueValue(string attribute)
        {
            int pos = this.OriginalDocument.IndexOf("\"" + attribute + "\"", (int)this.Position);
            if (pos < 0)
                return null;

            int keySeparator = this.OriginalDocument.IndexOf(":", pos);
            if (keySeparator < 0)
                return null;

            int opening = this.OriginalDocument.IndexOf("\"", keySeparator);
            if (opening < 0)
                return null;
            opening++;
            int closing = this.OriginalDocument.IndexOf("\"", opening + 1);
            if (closing < 0)
                return null;

            if (closing <= opening)
                return null;

            var value = this.OriginalDocument.Substring(opening, closing - opening);
            return value;
        }

        public string GetPreviousQuotedString(int position)
        {
            int endingQuote = this.OriginalDocument.LastIndexOf('"', position);
            if (endingQuote == -1)
                return null;

            int startingQuote = this.OriginalDocument.LastIndexOf('"', endingQuote - 1);
            if (startingQuote == -1 || startingQuote + 1 == endingQuote)
                return null;

            string token = this.OriginalDocument.Substring(startingQuote + 1, endingQuote - startingQuote - 1);

            return token;
        }

        public void GetPreviousNonWhiteChar(int position, out char? prevChar, out int? foundPosition)
        {
            prevChar = null;
            foundPosition = null;

            for (int currentPosition = position - 1; currentPosition >= 0; currentPosition--)
            {
                if (!char.IsWhiteSpace(this.OriginalDocument[currentPosition]))
                {
                    prevChar = this.OriginalDocument[currentPosition];
                    foundPosition = currentPosition;
                    return;
                }
            }
        }
        #region Data Structures
        /// <summary>
        /// Represents a token in the json document, with the TokenType and value if applicable.
        /// </summary>
        public class JsonToken
        {
            public JsonToken()
            {
                Position = -1;
            }

            public JsonTokenType Type { get; set; }
            public string Text { get; set; }
            public int Position { get; set; }
            public int Length { get; set; }

            public override string ToString()
            {
                return string.Format("{0}: {1}", this.Type, this.Text);
            }
        }

        /// <summary>
        /// The different token types in the json document
        /// </summary>
        public enum JsonTokenType
        {
            None,
            StartElement,
            EndElement,
            ElementSeperator,
            Text,
            KeyValueSeperator,
            StartArray,
            EndArray,
            Number,
            Boolean,
            Null
        }
        #endregion
    }
}
