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
    /// JSON uses Arrays and Objects. These correspond here to the data types ArrayList and Hashtable.
    /// All numbers are parsed to doubles.
    /// </summary>
    public class JsonParser
    {
        private enum Token
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

        private readonly ContextTextReader _json;
        private readonly StringBuilder _sb = new(); // common builder for building partials
        private readonly bool _ignoreCase;
        private Token _lookAheadToken = Token.None;
        private char _lookAheadChar = '\0';
        private int _index; // string index. Only used for reporting back error positions

        /// <summary>
        /// Create a parser for an JSON string loaded in memory
        /// </summary>
        /// <param name="json">The input JSON string</param>
        /// <param name="ignoreCase">If `true`, all property names will be lowercased</param>
        public JsonParser(string json, bool ignoreCase)
        {
            _json = new ContextTextReader(json);
            _ignoreCase = ignoreCase;
        }

        /// <summary>
        /// Create a parser for an JSON string accessible as a stream
        /// </summary>
        /// <param name="json">The input JSON stream</param>
        /// <param name="ignoreCase">If `true`, all property names will be lowercased</param>
        /// <param name="encoding">String encoding to use</param>
        public JsonParser(Stream json, bool ignoreCase, Encoding? encoding)
        {
            _json = new ContextTextReader(json, encoding ?? Json.DefaultStreamEncoding);
            _ignoreCase = ignoreCase;
        }

        /// <summary>
        /// Create a parser for an JSON byte array loaded in memory
        /// </summary>
        /// <param name="json">The input JSON byte array</param>
        /// <param name="ignoreCase">If `true`, all property names will be lowercased</param>
        /// <param name="encoding">Encoding of the bytes. Defaults to UTF8</param>
        public JsonParser(byte[] json, bool ignoreCase, Encoding? encoding)
        {
            var jsonBytesString = (encoding ?? Encoding.UTF8).GetString(json);
            _json = new ContextTextReader(jsonBytesString);
            _ignoreCase = ignoreCase;
        }

        /// <summary>
        /// If set to true, numeric values will be parsed as high-precision types.
        /// Otherwise, numeric values are parsed as double-precision floats.
        /// </summary>
        public bool UseWideNumbers { get; set; }

        /// <summary>
        /// Decode the provided JSON into an object representation
        /// </summary>
        public object? Decode()
        {
            return ParseValue();
        }

	    private Dictionary<string, object?> ParseObject()
        {
			var table = new Dictionary<string, object?>();

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
                            var name = ParseString();
                            if (_ignoreCase) name = Json.NormaliseCase(name);

                            // :
                            if (NextToken() != Token.Colon)
                            {
                                throw new Exception($"Expected colon at index {_index}; {_json.GetContext()}");
                            }

                            // value
                            var value = ParseValue();
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
                        throw new Exception($"Parser state exception at {_index}: advanced too far in an array; {_json.GetContext()}");

                    default:
                        {
                            array.Add(ParseValue());
                        }
                        break;
                }
            }
        }

        private object? ParseValue()
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
                
                case Token.None:
                case Token.CurlyClose:
                case Token.SquaredClose:
                case Token.Colon:
                case Token.Comma:
                case Token.Comment:
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new Exception($"Unrecognized token '{_lookAheadChar}' at index {_index} while looking for a value; {_json.GetContext()}");
        }

        private string ParseString()
        {
            ConsumeToken(); // "

            _sb.Length = 0;

            while (true)
            {
                _index++;
                var next = _json.Read();
                if (next <= 0) break;
                var c = (char)next;
                _lookAheadChar = c;

                if (c == '"') // end of string, not in escape
                {
                    return _sb.ToString();
                }

                if (c != '\\') // not end, not escape
                {
                    _sb.Append(c);
                    continue;
                }

                // now we are in an escape sequence
                
                // grab the escape char
                _index++;
                next = _json.Read();
                if (next <= 0) break;
                var c2 = (char)next;
                _lookAheadChar = c2;

                switch (c2)
                {
                    case '"':
                        _sb.Append('"');
                        break;

                    case '\\':
                        _sb.Append('\\');
                        break;

                    case '/':
                        _sb.Append('/');
                        break;

                    case 'b':
                        _sb.Append('\b');
                        break;

                    case 'f':
                        _sb.Append('\f');
                        break;

                    case 'n':
                        _sb.Append('\n');
                        break;

                    case 'r':
                        _sb.Append('\r');
                        break;

                    case 't':
                        _sb.Append('\t');
                        break;

                    case 'u':
                        {
                            var ua = _json.Read();
                            var ub = _json.Read();
                            var uc = _json.Read();
                            var ud = _json.Read();

                            if (ua <= 0 || ub <= 0 || uc <= 0 || ud <= 0) break;

                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint = ParseUnicode(ua, ub, uc, ud);
                            _sb.Append((char)codePoint);

                            // skip 4 chars
                            _index += 4;
                        }
                        break;
                }
            }

            throw new Exception($"Unexpectedly reached end of string value; {_json.GetContext()}");
        }

        private static uint ParseSingleChar(int c1, uint multiplier)
        {
            uint p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (uint)(c1 - '0') * multiplier;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (uint)((c1 - 'A') + 10) * multiplier;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (uint)((c1 - 'a') + 10) * multiplier;
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

        private object ParseNumber()
        {
            _sb.Length = 0; // reset string builder
            _sb.Append(_lookAheadChar); // include first character
            ConsumeToken();

            do
            {
                var next = _json.Peek();
                if (next <= 0) throw new Exception($"Unexpected end of string whilst parsing number; {_json.GetContext()}");
                var c = (char)next;
                _lookAheadChar = c;

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    _sb.Append(c);
                    _json.Read();
                    _index++;
                    continue;
                }
                break;
            } while (true);

            if (UseWideNumbers)
            {
                if (WideNumber.TryParse(_sb.ToString(), out var result)) return result;
            }
            else
            {
                if (double.TryParse(_sb.ToString(), out var result)) return result;
            }

            throw new Exception($"Incorrect number format at {_index}: '{_sb}'; {_json.GetContext()}");
        }

        private Token LookAhead()
        {
            if (_lookAheadToken != Token.None) return _lookAheadToken;

            return _lookAheadToken = NextTokenCore();
        }

        private void ConsumeToken()
        {
            _lookAheadToken = Token.None;
        }

        private void ConsumeLine()
        {
            // Skip until new line
            while (true)
            {
                
                _index++;
                var next = _json.Read();
                if (next <= 0) break;
                _lookAheadChar = (char)next;

                if (_lookAheadChar == '\n' || _lookAheadChar == '\r') break;

            }
            _lookAheadToken = Token.None;
        }

        private Token NextToken()
        {
            var result = _lookAheadToken != Token.None ? _lookAheadToken : NextTokenCore();

            _lookAheadToken = Token.None;

            return result;
        }

        private Token NextTokenCore()
        {
            int next;

            // Read next non-whitespace char
            while (true)
            {
                next = _json.Read();
                if (next <= 0) break;

                _index++;
                if (next == 0xFEFF) continue; // BOM

                _lookAheadChar = (char)next;

                var c = _lookAheadChar;
                if (c > ' ') break;
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;
            }
            
            if (next <= 0) throw new Exception($"Reached end of input unexpectedly; {_json.GetContext()}");

            switch (_lookAheadChar)
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
                        _index += 4;
                        return Token.False;
                    }
                    break;

                case 't':
                    if (NextCharsAre('r', 'u', 'e'))
                    {
                        _index += 3;
                        return Token.True;
                    }
                    break;

                case 'n':
                    if (NextCharsAre('u', 'l', 'l'))
                    {
                        _index += 3;
                        return Token.Null;
                    }
                    break;

                case '/':
                    if (NextCharsAre('/')) // double slash line comment (not standard JSON)
                    {
                        _index += 1;
                        return Token.Comment;
                    }
                    break;

            }

            throw new Exception($"Could not find token at index {--_index}; Got '{_lookAheadChar}' (0x{(int)_lookAheadChar:X2}); {_json.GetContext()}");
        }

        private bool NextCharsAre(params char[] cs)
        {
            foreach (var t in cs)
            {
                var next = _json.Read();
                if (next <= 0) return false;
                _lookAheadChar = (char)next;
                if (_lookAheadChar != t) return false;
            }
            return true;
        }
    }
}
