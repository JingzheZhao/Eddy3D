using System.Reflection;
using System.Text.RegularExpressions;
using EddyLib;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private static Dictionary<Guid, ComponentIoDefinition> LoadCurrentCommitComponentIoDefinitions(string componentsRoot)
    {
        var definitions = new Dictionary<Guid, ComponentIoDefinition>();

        foreach (var file in Directory.GetFiles(componentsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (!TryParseComponentIoDefinition(file, out var definition))
            {
                continue;
            }

            if (!definitions.ContainsKey(definition.ComponentGuid))
            {
                definitions.Add(definition.ComponentGuid, definition);
            }
        }

        return definitions;
    }

    private static bool TryParseComponentIoDefinition(string sourcePath, out ComponentIoDefinition definition)
    {
        definition = default!;

        var source = File.ReadAllText(sourcePath);
        var stripped = StripComments(source);

        var guid = ExtractComponentGuid(stripped);
        if (!guid.HasValue)
        {
            return false;
        }

        definition = new ComponentIoDefinition
        {
            ComponentGuid = guid.Value,
            Inputs = ExtractParameterDefinitions(stripped, "RegisterInputParams"),
            Outputs = ExtractParameterDefinitions(stripped, "RegisterOutputParams")
        };

        return true;
    }

    private static Guid? ExtractComponentGuid(string source)
    {
        var idx = source.IndexOf("ComponentGuid", StringComparison.Ordinal);
        if (idx < 0)
        {
            return null;
        }

        var tail = source[idx..];
        var guidStart = tail.IndexOf("new Guid(", StringComparison.Ordinal);
        if (guidStart < 0)
        {
            return null;
        }

        var openQuote = tail.IndexOf('"', guidStart);
        if (openQuote < 0)
        {
            return null;
        }

        var closeQuote = tail.IndexOf('"', openQuote + 1);
        if (closeQuote <= openQuote)
        {
            return null;
        }

        var raw = tail[(openQuote + 1)..closeQuote].Trim();
        return Guid.TryParse(raw, out var guid) ? guid : null;
    }

    private static List<ParameterDefinition> ExtractParameterDefinitions(string source, string methodName)
    {
        var parameters = new List<ParameterDefinition>();

        var methodIdx = source.IndexOf(methodName, StringComparison.Ordinal);
        if (methodIdx < 0)
        {
            return parameters;
        }

        var openBrace = source.IndexOf('{', methodIdx);
        if (openBrace < 0)
        {
            return parameters;
        }

        var closeBrace = FindMatchingBracket(source, openBrace, '{', '}');
        if (closeBrace <= openBrace)
        {
            return parameters;
        }

        var body = source.Substring(openBrace + 1, closeBrace - openBrace - 1);
        var cursor = 0;
        while (cursor < body.Length)
        {
            var addIdx = body.IndexOf("pManager.Add", cursor, StringComparison.Ordinal);
            if (addIdx < 0)
            {
                break;
            }

            var openParen = body.IndexOf('(', addIdx);
            if (openParen < 0)
            {
                break;
            }

            var closeParen = FindMatchingBracket(body, openParen, '(', ')');
            if (closeParen < 0)
            {
                break;
            }

            var args = SplitTopLevelArguments(body.Substring(openParen + 1, closeParen - openParen - 1));
            if (args.Count >= 2)
            {
                var name = ResolveParameterToken(args[0]) ?? string.Empty;
                var nick = ResolveParameterToken(args[1]) ?? string.Empty;
                parameters.Add(new ParameterDefinition { Name = name, NickName = nick });
            }

            cursor = closeParen + 1;
        }

        return parameters;
    }

    private static string? ResolveParameterToken(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var token = expression.Trim();

        if (token.StartsWith("@\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal))
        {
            return token.Substring(2, token.Length - 3).Replace("\"\"", "\"");
        }

        if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal))
        {
            return Regex.Unescape(token.Substring(1, token.Length - 2));
        }

        if (token.StartsWith("nameof(", StringComparison.Ordinal) && token.EndsWith(")", StringComparison.Ordinal))
        {
            var inner = token.Substring("nameof(".Length, token.Length - "nameof(".Length - 1).Trim();
            var lastDot = inner.LastIndexOf('.');
            return lastDot >= 0 ? inner[(lastDot + 1)..] : inner;
        }

        if (token.StartsWith("EddyLib.GH_Strings.", StringComparison.Ordinal))
        {
            token = token["EddyLib.".Length..];
        }

        if (!token.StartsWith("GH_Strings.", StringComparison.Ordinal))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 3)
        {
            return null;
        }

        Type? current = typeof(GH_Strings);
        for (var i = 1; i < parts.Length - 1; i++)
        {
            current = current.GetNestedType(parts[i], BindingFlags.Public);
            if (current == null)
            {
                return null;
            }
        }

        var field = current.GetField(parts[^1], BindingFlags.Public | BindingFlags.Static);
        if (field == null)
        {
            return null;
        }

        var value = field.GetValue(null) as string;
        if (value != null)
        {
            return value;
        }

        return field.IsLiteral ? field.GetRawConstantValue()?.ToString() : null;
    }

}
