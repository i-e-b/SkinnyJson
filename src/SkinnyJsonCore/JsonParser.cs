using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkinnyJson
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// 
    /// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
    /// All numbers are parsed to doubles.
    /// </summary>
    public class JsonParser
    {
        enum Token
        {
            None = -1,           // Used to denote no Lookahead available
            CurlyOpen,
            CurlyClose,
            SquaredOpen,
            SquaredClose,
            Colon,
            Comma,
            String,
            Number,
            True,
            False,
            Null,
            Comment
        }

        readonly TextReader json;
        readonly StringBuilder s = new StringBuilder(); // common builder for building partials
    	readonly bool ignorecase;
        Token lookAheadToken = Token.None;
        char lookAheadChar = '\0';
        int index; // string index. Only used for reporting back error positions

        /// <summary>
        /// Create a parser for an JSON string loaded in memory
        /// </summary>
        /// <param name="json">The input JSON string</param>
        /// <param name="ignorecase">If `true`, all property names will be lowercased</param>
        public JsonParser(string json, bool ignorecase)
        {
            this.json = new StringReader(json);
            this.ignorecase = ignorecase;
        }

        /// <summary>
        /// Create a parser for an JSON string accessible as a stream
        /// </summary>
        /// <param name="json">The input JSON stream</param>
        /// <param name="ignorecase">If `true`, all property names will be lowercased</param>
        /// <param name="encoding">String encoding to use</param>
        public JsonParser(Stream json, bool ignorecase, Encoding encoding)
        {
            this.json = new StreamReader(json, encoding);
            this.ignorecase = ignorecase;
        }

        /// <summary>
        /// Create a parser for an JSON byte array loaded in memory
        /// </summary>
        /// <param name="json">The input JSON byte array</param>
        /// <param name="ignorecase">If `true`, all property names will be lowercased</param>
        public JsonParser(byte[] json, bool ignorecase)
        {
            var jsonBytesString = Encoding.UTF8.GetString(json);
            this.json = new StringReader(jsonBytesString);
            this.ignorecase = ignorecase;
        }

        /// <summary>
        /// Decode the provided JSON into an object representation
        /// </summary>
        public object Decode()
        {
            return ParseValue();
        }

	    private Dictionary<string, object> ParseObject()
        {
			var table = new Dictionary<string, object>();

	        ConsumeToken(); // {

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comment:
                        ConsumeLine();
                        break;

                    case Token.Comma:
                        ConsumeToken();
                        break;

                    case Token.CurlyClose:
                        ConsumeToken();
                        return table;

                    default:
                        {

                            // name
                            string name = ParseString();
                            if (ignorecase)
                                name = name.ToLower();

                            // :
                            if (NextToken() != Token.Colon)
                            {
                                throw new Exception("Expected colon at index " + index);
                            }

                            // value
                            object value = ParseValue();

                            table[name] = value;
                        }
                        break;
                }
            }
        }

        private ArrayList ParseArray()
        {
            var array = new ArrayList();
            ConsumeToken(); // [

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comment:
                        ConsumeLine();
                        break;

                    case Token.Comma:
                        ConsumeToken();
                        break;

                    case Token.SquaredClose:
                        ConsumeToken();
                        return array;

                    case Token.CurlyClose:
                        throw new Exception("Parser state exception at " + index + ": advanced too far in an array.");

                    default:
                        {
                            array.Add(ParseValue());
                        }
                        break;
                }
            }
        }

        private object ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    return ParseString();

                case Token.CurlyOpen:
                    return ParseObject();

                case Token.SquaredOpen:
                    return ParseArray();

                case Token.True:
                    ConsumeToken();
                    return true;

                case Token.False:
                    ConsumeToken();
                    return false;

                case Token.Null:
                    ConsumeToken();
                    return null;
            }

            throw new Exception("Unrecognized token '" + lookAheadChar + "' at index " + index + " while looking for a value");
        }

        private string ParseString()
        {
            ConsumeToken(); // "

            s.Length = 0;

            while (true)
            {
                index++;
                var next = json.Read();
                if (next <= 0) break;
                var c = (char)next;
                lookAheadChar = c;

                if (c == '"') // end of string, not in escape
                {
                    return s.ToString();
                }

                if (c != '\\') // not end, not escape
                {
                    s.Append(c);
                    continue;
                }

                // now we are in an escape sequence
                
                // grab the escape char
                index++;
                next = json.Read();
                if (next <= 0) break;
                var c2 = (char)next;
                lookAheadChar = c2;

                switch (c2)
                {
                    case '"':
                        s.Append('"');
                        break;

                    case '\\':
                        s.Append('\\');
                        break;

                    case '/':
                        s.Append('/');
                        break;

                    case 'b':
                        s.Append('\b');
                        break;

                    case 'f':
                        s.Append('\f');
                        break;

                    case 'n':
                        s.Append('\n');
                        break;

                    case 'r':
                        s.Append('\r');
                        break;

                    case 't':
                        s.Append('\t');
                        break;

                    case 'u':
                        {
                            var ua = json.Read();
                            var ub = json.Read();
                            var uc = json.Read();
                            var ud = json.Read();

                            if (ua <= 0 || ub <= 0 || uc <= 0 || ud <= 0) break;

                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint = ParseUnicode(ua, ub, uc, ud);
                            s.Append((char)codePoint);

                            // skip 4 chars
                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("Unexpectedly reached end of string value");
        }

        private static uint ParseSingleChar(int c1, uint multipliyer)
        {
            uint p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (uint)(c1 - '0') * multipliyer;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (uint)((c1 - 'A') + 10) * multipliyer;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (uint)((c1 - 'a') + 10) * multipliyer;
            return p1;
        }

        private uint ParseUnicode(int c1, int c2, int c3, int c4)
        {
            uint p1 = ParseSingleChar(c1, 0x1000);
            uint p2 = ParseSingleChar(c2, 0x100);
            uint p3 = ParseSingleChar(c3, 0x10);
            uint p4 = ParseSingleChar(c4, 1);

            return p1 + p2 + p3 + p4;
        }

        private double ParseNumber()
        {
            s.Length = 0; // reset string builder
            s.Append(lookAheadChar); // include first character
            ConsumeToken();

            do
            {
                var next = json.Peek();
                if (next <= 0) throw new Exception("Unexpected end of string whilst parsing number");
                var c = (char)next;
                lookAheadChar = c;

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    s.Append(c);
                    json.Read();
                    index++;
                    continue;
                }
                break;
            } while (true);

            if (double.TryParse(s.ToString(), out var result)) return result;

            throw new Exception("Incorrect number format at "+index+": '"+s+"'");
        }

        private Token LookAhead()
        {
            if (lookAheadToken != Token.None) return lookAheadToken;

            return lookAheadToken = NextTokenCore();
        }

        private void ConsumeToken()
        {
            lookAheadToken = Token.None;
        }

        private void ConsumeLine()
        {
            // Skip until new line
            while (true)
            {
                
                index++;
                var next = json.Read();
                if (next <= 0) break;
                lookAheadChar = (char)next;

                if (lookAheadChar == '\n' || lookAheadChar == '\r') break;

            }
            lookAheadToken = Token.None;
        }

        private Token NextToken()
        {
            var result = lookAheadToken != Token.None ? lookAheadToken : NextTokenCore();

            lookAheadToken = Token.None;

            return result;
        }

        private Token NextTokenCore()
        {
            int next;

            // Read next non-whitespace char
            while (true)
            {
                next = json.Read();
                if (next <= 0) break;

                index++;
                if (next == 0xFEFF) continue; // BOM

                lookAheadChar = (char)next;

                var c = lookAheadChar;
                if (c > ' ') break;
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;
            }
            
            if (next <= 0) throw new Exception("Reached end of input unexpectedly");

            switch (lookAheadChar)
            {
                case '{':
                    return Token.CurlyOpen;

                case '}':
                    return Token.CurlyClose;

                case '[':
                    return Token.SquaredOpen;

                case ']':
                    return Token.SquaredClose;

                case ',':
                    return Token.Comma;

                case '"':
                    return Token.String;

				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
                case '-': case '+': case '.':
                    return Token.Number;

                case ':':
                    return Token.Colon;

                case 'f':
                    if (NextCharsAre('a', 'l', 's', 'e'))
                    {
                        index += 4;
                        return Token.False;
                    }
                    break;

                case 't':
                    if (NextCharsAre('r', 'u', 'e'))
                    {
                        index += 3;
                        return Token.True;
                    }
                    break;

                case 'n':
                    if (NextCharsAre('u', 'l', 'l'))
                    {
                        index += 3;
                        return Token.Null;
                    }
                    break;

                case '/':
                    if (NextCharsAre('/')) // double slash line comment (not standard JSON)
                    {
                        index += 1;
                        return Token.Comment;
                    }
                    break;

            }

            throw new Exception("Could not find token at index " + --index + "; Got '" + lookAheadChar + "' "+((int)lookAheadChar).ToString("X2"));
        }

        private bool NextCharsAre(params char[] cs)
        {
            for (int i = 0; i < cs.Length; i++)
            {
                var next = json.Read();
                if (next <= 0) return false;
                lookAheadChar = (char)next;
                if (lookAheadChar != cs[i]) return false;
            }
            return true;
        }
    }
}
