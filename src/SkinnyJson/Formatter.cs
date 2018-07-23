using System.Text;

namespace SkinnyJson
{
    internal static class Formatter
    {
        public static string Indent = "    ";

        public static void AppendIndent(StringBuilder sb, int count)
        {
            for (; count > 0; --count) sb.Append(Indent);
        }

        public static bool IsEscaped(StringBuilder sb, int index)
        {
            bool escaped = false;
            while (index > 0 && index < sb.Length) {
                if (sb[index - 1] == '\\') {
                    escaped = !escaped;
                    index--;
                } else break;
            }
            return escaped;
        }

        public static string PrettyPrint(string input)
        {
            var output = new StringBuilder(input.Length * 2);
            char? quote = null;
            bool inComment = false;
            int depth = 0;

            for (int i = 0; i < input.Length; ++i)
            {
                char ch = input[i];

                if (inComment && ch != '\r' && ch != '\n') continue;
                inComment = false;

                switch (ch)
                {
                    case '{':
                    case '[':
                        output.Append(ch);
                        if (!quote.HasValue)
                        {
                            output.AppendLine();
                            AppendIndent(output, ++depth);
                        }
                        break;
                    case '}':
                    case ']':
                        if (quote.HasValue)
                            output.Append(ch);
                        else
                        {
                            output.AppendLine();
                            AppendIndent(output, --depth);
                            output.Append(ch);
                        }
                        break;
                    case '"':
                    case '\'':
                        output.Append(ch);
                        if (quote.HasValue)
                        {
                            if (!IsEscaped(output, i) && ch == quote)
                                quote = null;
                        }
                        else quote = ch;
                        break;
                    case ',':
                        output.Append(ch);
                        if (!quote.HasValue)
                        {
                            output.AppendLine();
                            AppendIndent(output, depth);
                        }
                        break;
                    case ':':
                        if (quote.HasValue) output.Append(ch);
                        else output.Append(" : ");
                        break;
                    case '/':
                        if (!quote.HasValue && (i + 1 < input.Length))
                        {
                            if (input[i+1] == '/') inComment = true;// line comment
                        } else output.Append(ch);
                        break;
                    default:
                        if (quote.HasValue || !char.IsWhiteSpace(ch))
                            output.Append(ch);
                        break;
                }
            }

            return output.ToString();
        }
    }
}