namespace LabServer.Server.Service;

using System.Text;
using System.Text.RegularExpressions;

public class CodeNormalizer
{
    private static readonly Regex s_tokenRegex = new(
        @"[A-Za-z_][A-Za-z0-9_]*|\d+(?:\.\d+)?|==|!=|<=|>=|&&|\|\||->|[{}()\[\];,=+\-*/%<>!&|^~?:.]",
        RegexOptions.Compiled);

    private static readonly HashSet<System.String> s_keywords = new(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do", "double",
        "else", "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long",
        "register", "restrict", "return", "short", "signed", "sizeof", "static", "struct",
        "switch", "typedef", "union", "unsigned", "void", "volatile", "while", "_Bool",
        "_Complex", "_Imaginary"
    };

    public IReadOnlyList<System.String> NormalizeToTokens(System.String source)
    {
        var withoutComments = RemoveCComments(source);
        var result = new List<System.String>();

        foreach (Match match in s_tokenRegex.Matches(withoutComments))
        {
            var token = match.Value;
            if (token.Length == 0)
                continue;

            if (char.IsDigit(token[0]))
            {
                result.Add("NUM");
                continue;
            }

            if (IsIdentifier(token) && !s_keywords.Contains(token))
            {
                result.Add("ID");
                continue;
            }

            result.Add(token);
        }

        return result;
    }

    public System.String RemoveCComments(System.String source)
    {
        var output = new StringBuilder(source.Length);
        var state = CommentStripState.Code;
        var escaped = false;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            switch (state)
            {
                case CommentStripState.Code:
                    if (c == '/' && next == '/')
                    {
                        i++;
                        state = CommentStripState.LineComment;
                    }
                    else if (c == '/' && next == '*')
                    {
                        i++;
                        state = CommentStripState.BlockComment;
                    }
                    else
                    {
                        output.Append(c);
                        if (c == '"')
                        {
                            state = CommentStripState.StringLiteral;
                            escaped = false;
                        }
                        else if (c == '\'')
                        {
                            state = CommentStripState.CharLiteral;
                            escaped = false;
                        }
                    }
                    break;

                case CommentStripState.LineComment:
                    if (c == '\\' && IsNewLine(next))
                    {
                        i++;
                    }
                    else if (IsNewLine(c))
                    {
                        output.Append(c);
                        state = CommentStripState.Code;
                    }
                    break;

                case CommentStripState.BlockComment:
                    if (c == '*' && next == '/')
                    {
                        i++;
                        state = CommentStripState.Code;
                    }
                    else if (IsNewLine(c))
                    {
                        output.Append(c);
                    }
                    break;

                case CommentStripState.StringLiteral:
                    output.Append(c);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        state = CommentStripState.Code;
                    }
                    break;

                case CommentStripState.CharLiteral:
                    output.Append(c);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '\'')
                    {
                        state = CommentStripState.Code;
                    }
                    break;
            }
        }

        return output.ToString();
    }

    private static System.Boolean IsIdentifier(System.String token)
        => token.Length > 0 && (char.IsLetter(token[0]) || token[0] == '_');

    private static System.Boolean IsNewLine(System.Char c) => c is '\n' or '\r';

    private enum CommentStripState
    {
        Code,
        LineComment,
        BlockComment,
        StringLiteral,
        CharLiteral
    }
}
