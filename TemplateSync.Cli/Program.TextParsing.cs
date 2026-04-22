using System.Text;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private static string StripComments(string source)
    {
        var sb = new StringBuilder(source.Length);
        var inString = false;
        var inVerbatim = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n')
                {
                    inLineComment = false;
                    sb.Append(c);
                }
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }
                continue;
            }

            if (inString)
            {
                sb.Append(c);
                if (inVerbatim)
                {
                    if (c == '"' && next == '"')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                        inVerbatim = false;
                    }
                }
                else
                {
                    if (c == '\\' && next != '\0')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                    }
                }

                continue;
            }

            if (c == '/' && next == '/')
            {
                inLineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            if (c == '@' && next == '"')
            {
                inString = true;
                inVerbatim = true;
                sb.Append(c);
                sb.Append(next);
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                inVerbatim = false;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static List<string> SplitTopLevelArguments(string argList)
    {
        var args = new List<string>();
        var sb = new StringBuilder();
        var parenDepth = 0;
        var braceDepth = 0;
        var bracketDepth = 0;
        var inString = false;
        var inVerbatim = false;

        for (var i = 0; i < argList.Length; i++)
        {
            var c = argList[i];
            var next = i + 1 < argList.Length ? argList[i + 1] : '\0';

            if (inString)
            {
                sb.Append(c);

                if (inVerbatim)
                {
                    if (c == '"' && next == '"')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                        inVerbatim = false;
                    }
                }
                else
                {
                    if (c == '\\' && next != '\0')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                    }
                }

                continue;
            }

            if (c == '@' && next == '"')
            {
                inString = true;
                inVerbatim = true;
                sb.Append(c);
                sb.Append(next);
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                inVerbatim = false;
                sb.Append(c);
                continue;
            }

            switch (c)
            {
                case '(':
                    parenDepth++;
                    sb.Append(c);
                    break;
                case ')':
                    parenDepth--;
                    sb.Append(c);
                    break;
                case '{':
                    braceDepth++;
                    sb.Append(c);
                    break;
                case '}':
                    braceDepth--;
                    sb.Append(c);
                    break;
                case '[':
                    bracketDepth++;
                    sb.Append(c);
                    break;
                case ']':
                    bracketDepth--;
                    sb.Append(c);
                    break;
                case ',' when parenDepth == 0 && braceDepth == 0 && bracketDepth == 0:
                    args.Add(sb.ToString().Trim());
                    sb.Clear();
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        if (sb.Length > 0)
        {
            args.Add(sb.ToString().Trim());
        }

        return args;
    }

    private static int FindMatchingBracket(string text, int openIndex, char openBracket, char closeBracket)
    {
        var depth = 0;
        var inString = false;
        var inVerbatim = false;

        for (var i = openIndex; i < text.Length; i++)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (inString)
            {
                if (inVerbatim)
                {
                    if (c == '"' && next == '"')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                        inVerbatim = false;
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                    }
                }

                continue;
            }

            if (c == '@' && next == '"')
            {
                inString = true;
                inVerbatim = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                inVerbatim = false;
                continue;
            }

            if (c == openBracket)
            {
                depth++;
            }
            else if (c == closeBracket)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

}
