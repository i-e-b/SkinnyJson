using System.IO;
using System.Text;

namespace SkinnyJson
{
    internal static class Formatter
    {
        public static string Indent = "    ";

        public static void AppendIndent(TextWriter sb, int count)
        {
            for (; count > 0; --count) sb.Write(Indent);
        }

        public static void PrettyStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding)
        {
            PrettyPrintInternal(new StreamReader(input, inputEncoding), new StreamWriter(output, outputEncoding));
        }

        
        public static string PrettyPrint(string input)
        {
            var output = new StringBuilder(input.Length * 2);
            PrettyPrintInternal(new StringReader(input), new StringWriter(output));
            return output.ToString();
        }

        private static void PrettyPrintInternal(TextReader input, TextWriter output)
        {
            char? quote = null;
            bool inComment = false;
            bool isEscaped = false;
            int depth = 0;

            while (true)
            {
                int v = input.Read();
                if (v < 0) {
                    output.Flush();
                    return;
                }
                char ch = (char)v;

                if (inComment && ch != '\r' && ch != '\n') continue;
                inComment = false;

                if (isEscaped) {
                    output.Write(ch);
                    isEscaped = false;
                    continue;
                }

                switch (ch)
                {
                    case '{':
                    case '[':
                        output.Write(ch);
                        if (!quote.HasValue)
                        {
                            output.WriteLine();
                            AppendIndent(output, ++depth);
                        }
                        break;
                    case '}':
                    case ']':
                        if (quote.HasValue)
                            output.Write(ch);
                        else
                        {
                            output.WriteLine();
                            AppendIndent(output, --depth);
                            output.Write(ch);
                        }
                        break;
                    case '"':
                    case '\'':
                        output.Write(ch);
                        if (quote.HasValue)
                        {
                            if (ch == quote) {
                                quote = null;
                            }
                        }
                        else quote = ch;
                        break;
                    case '\\':
                        output.Write(ch);
                        if (quote.HasValue) isEscaped = true;
                        break;
                    case ',':
                        output.Write(ch);
                        if (!quote.HasValue)
                        {
                            output.WriteLine();
                            AppendIndent(output, depth);
                        }
                        break;
                    case ':':
                        if (quote.HasValue) output.Write(ch);
                        else output.Write(" : ");
                        break;
                    case '/':
                        if (!quote.HasValue)
                        {
                            if (input.Peek() == '/') inComment = true; // line comment
                        } else output.Write(ch);
                        break;
                    default:
                        if (quote.HasValue || !char.IsWhiteSpace(ch))
                            output.Write(ch);
                        break;
                }
            }
        }
    }
}