using System.IO;
using System.Text;

namespace SkinnyJson
{
    /// <summary>
    /// Text reader that keeps a small buffer of the start and end of data that has been read.
    /// This is used to improve error messages
    /// </summary>
    internal class ContextTextReader : TextReader
    {
        private readonly TextReader _internal;

        private const int Len = 1 << 4; // buffer length, must be power of two
        private const int Mask = Len - 1; // length-mod mask.
        
        private readonly char[] _start = new char[Len];
        private readonly char[] _end = new char[Len];
        private int _length; // how many characters read?
        
        public ContextTextReader(string src)
        {
            _internal = new StringReader(src);
        }

        public ContextTextReader(Stream src, Encoding encoding)
        {
            _internal = new StreamReader(src, encoding);
        }

        public override int Peek()
        {
            return _internal.Peek();
        }

        public override int Read()
        {
            var x = _internal.Read();
            if (x < 0) return x; // no character

            var p = _length & Mask;
            if (_length < Len) _start[p] = (char)x;
            _end[p] = (char)x;
            _length++;
            
            return x;
        }

        public string GetContext()
        {
            if (_length < 1) return "Zero length input";
            
            // Try to read more characters (context past failure point)
            var offset = _length - 1;
            var extraContext = (_length >= (Len * 2)) ? (Len / 2) : (Len * 2 - _length);
            for (int i = 0; i < extraContext; i++)
            {
                var x = _internal.Read();
                if (x < 0) break;
                
                var p = _length & Mask;
                if (_length < Len) _start[p] = (char)x;
                _end[p] = (char)x;
                _length++;
            }

            var sb = new StringBuilder(Len * 2 + 4);
            
            if (_length <= Len) // haven't overflowed either buffer yet, just use end.
            {
                for (int i = 0; i < _length; i++)
                {
                    if (i == offset) sb.Append("\u035F");
                    sb.Append(_end[i]);
                }
                return sb.ToString();
            }

            
            var prefix = _length - Len;
            if (_length >= Len * 2) // start should have entirely unique values
            {
                for (int i = 0; i < Len; i++)
                {
                    if (i == offset) sb.Append("\u035F");
                    sb.Append(_start[i]);
                }
                if (_length > Len * 2) sb.Append("…"); // gap between start and end
            }
            else // there is an overlap between start and end context (both buffers should be full)
            {
                for (int i = 0; i < prefix; i++) sb.Append(_start[i]);
            }

            for (int i = 0; i < Len; i++)
            {
                var p = prefix + i;
                if (p == offset) sb.Append("\u035F");
                var j = p & Mask;
                sb.Append(_end[j]);
            }



            return sb.ToString();
        }
    }
}