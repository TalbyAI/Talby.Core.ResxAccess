using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal sealed class PlaceholderArgument
{
    public PlaceholderArgument(string identity, int? index, string typeName)
    {
        Identity = identity;
        Index = index;
        TypeName = typeName;
    }

    public string Identity { get; }
    public int? Index { get; }
    public string Name => Index is null ? Identity : "arg" + Identity;
    public string TypeName { get; }

    public Type Type => GetSupportedType(TypeName) ?? typeof(object);

    public static Type? GetSupportedType(string typeName) =>
        (
            typeName.EndsWith("?", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - 1)
                : typeName
        ) switch
        {
            "string" => typeof(string),
            "bool" => typeof(bool),
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "decimal" => typeof(decimal),
            "DateTime" => typeof(DateTime),
            "DateTimeOffset" => typeof(DateTimeOffset),
            "Guid" => typeof(Guid),
            _ => null,
        };
}

[CompileTime]
internal sealed class PlaceholderContract
{
    public PlaceholderArgument[] Arguments { get; }
    public Dictionary<string, string> Formats { get; } = new(StringComparer.Ordinal);

    private PlaceholderContract(PlaceholderArgument[] arguments)
    {
        Arguments = arguments;
    }

    public static PlaceholderContract Read(
        string text,
        string resourceName,
        string key,
        PlaceholderContract? reference = null
    )
    {
        var declarations = new Dictionary<string, (int? Index, string? Type)>(
            StringComparer.Ordinal
        );
        var occurrences = new List<(int Start, int End, string Identity, string Suffix)>();
        try
        {
            for (var position = 0; position < text.Length; position++)
            {
                var character = text[position];
                if (character is not ('{' or '}'))
                {
                    continue;
                }
                if (position + 1 < text.Length && text[position + 1] == character)
                {
                    position++;
                    continue;
                }
                if (character == '}')
                {
                    throw new FormatException("Unescaped closing brace.");
                }

                var opening = position++;
                var start = position;
                int? index = null;
                string identity;
                string? typeName = null;
                if (position < text.Length && text[position] is >= '0' and <= '9')
                {
                    var value = 0;
                    // Preserve the framework's bounded numeric scan, including leading zeros.
                    while (
                        position < text.Length
                        && text[position] is >= '0' and <= '9'
                        && value < 1_000_000
                    )
                    {
                        value = value * 10 + text[position++] - '0';
                    }
                    if (position < text.Length && text[position] is >= '0' and <= '9')
                    {
                        throw new FormatException(
                            "Indexed Placeholder identity exceeds the composite-format limit."
                        );
                    }
                    index = value;
                    identity = value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    while (
                        position < text.Length
                        && text[position] is not ('@' or ',' or ':' or '}' or '{' or ' ')
                    )
                    {
                        position++;
                    }
                    identity = text.Substring(start, position - start);
                    if (
                        !Regex.IsMatch(
                            identity,
                            @"\A[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*\z"
                        )
                    )
                    {
                        throw new FormatException(
                            $"Invalid Named Placeholder identifier '{DescribeIdentifier(identity)}'."
                        );
                    }
                    if (position < text.Length && text[position] == '@')
                    {
                        start = ++position;
                        while (
                            position < text.Length
                            && text[position] is not (',' or ':' or '}' or '{' or ' ')
                        )
                        {
                            position++;
                        }
                        typeName = text.Substring(start, position - start);
                        if (PlaceholderArgument.GetSupportedType(typeName) is null)
                        {
                            throw new FormatException(
                                $"Unsupported Argument Type '{typeName}' for Named Placeholder '{identity}'."
                            );
                        }
                    }
                }
                var suffixStart = position;
                while (position < text.Length && text[position] != '}')
                {
                    if (text[position] == '{')
                    {
                        throw new FormatException("Nested opening brace.");
                    }
                    position++;
                }
                if (position == text.Length)
                {
                    throw new FormatException("Unclosed Formatting Placeholder.");
                }
                var suffix = text.Substring(suffixStart, position - suffixStart + 1);
                // Validate alignment and format syntax without evaluating Argument Formats.
                string.Format(CultureInfo.InvariantCulture, "{0" + suffix, new object?[] { null });
                if (declarations.TryGetValue(identity, out var previous))
                {
                    if (
                        previous.Type is not null
                        && typeName is not null
                        && previous.Type != typeName
                    )
                    {
                        throw new FormatException(
                            $"Conflicting Argument Types '{previous.Type}' and '{typeName}' for Named Placeholder '{identity}'."
                        );
                    }
                    typeName ??= previous.Type;
                }
                declarations[identity] = (index, typeName);
                occurrences.Add((opening, position, identity, suffix));
            }

            var arguments = declarations
                .Where(pair => pair.Value.Index is null)
                .Concat(
                    declarations
                        .Where(pair => pair.Value.Index is not null)
                        .OrderBy(pair => pair.Value.Index)
                )
                .Select(pair => new PlaceholderArgument(
                    pair.Key,
                    pair.Value.Index,
                    pair.Value.Type ?? "object?"
                ))
                .ToArray();
            if (reference is not null)
            {
                if (
                    !new HashSet<string>(declarations.Keys, StringComparer.Ordinal).SetEquals(
                        reference.Arguments.Select(argument => argument.Identity)
                    )
                )
                {
                    var identities =
                        reference.Arguments.Length == 0
                            ? "(none)"
                            : string.Join(
                                ", ",
                                reference.Arguments.Select(argument => argument.Identity)
                            );
                    var style = reference.Arguments.All(argument => argument.Index is not null)
                        ? "Indexed Placeholders"
                        : "arguments";
                    throw new ResourceValidationException(
                        $"'{resourceName}' Resource Key '{key}' must use exactly the Reference Resource's Placeholder Contract ({style}: {identities})."
                    );
                }
                foreach (var argument in reference.Arguments)
                {
                    var explicitType = declarations[argument.Identity].Type;
                    if (explicitType is not null && explicitType != argument.TypeName)
                    {
                        throw new FormatException(
                            $"Named Placeholder '{argument.Identity}' declares Argument Type '{explicitType}'; the Reference Resource requires '{argument.TypeName}'."
                        );
                    }
                }
                arguments = reference.Arguments;
            }
            var parameterNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "resourceCulture",
                "formattingCulture",
            };
            foreach (var argument in arguments)
            {
                // C# ignores Unicode formatting characters when comparing identifiers.
                if (!parameterNames.Add(Regex.Replace(argument.Name, @"\p{Cf}", "")))
                {
                    throw new FormatException(
                        $"Named Placeholder '{DescribeIdentifier(argument.Name)}' collides with generated parameter '{DescribeIdentifier(argument.Name)}'."
                    );
                }
            }
            var contract = new PlaceholderContract(arguments);
            var format = new StringBuilder();
            var copied = 0;
            foreach (var occurrence in occurrences)
            {
                format.Append(text, copied, occurrence.Start - copied);
                format
                    .Append('{')
                    .Append(
                        Array
                            .FindIndex(
                                arguments,
                                argument => argument.Identity == occurrence.Identity
                            )
                            .ToString(CultureInfo.InvariantCulture)
                    )
                    .Append(occurrence.Suffix);
                copied = occurrence.End + 1;
            }
            format.Append(text, copied, text.Length - copied);
            contract.Formats.Add(text, format.ToString());
            return contract;
        }
        catch (FormatException exception)
        {
            throw new ResourceValidationException(
                $"'{resourceName}' Resource Key '{key}' has a malformed Formatting Placeholder: {exception.Message}"
            );
        }
    }

    private static string DescribeIdentifier(string identifier) =>
        string.Concat(
            identifier.Select(character =>
                char.IsControl(character)
                || char.GetUnicodeCategory(character) == UnicodeCategory.Format
                    ? "\\u" + ((int)character).ToString("X4", CultureInfo.InvariantCulture)
                    : character.ToString()
            )
        );
}
