using System.Globalization;
using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal static class IndexedPlaceholderContract
{
    // Named and mixed contracts keep Raw Text access until their dependent ticket.
    public static int[]? Read(string text, string resourceName, string key)
    {
        var arguments = new SortedSet<int>();
        var hasNamedPlaceholders = false;
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

                position++;
                var start = position;
                var index = 0;
                // Match string.Format's bounded numeric scan, including leading zeros.
                while (position < text.Length && text[position] is >= '0' and <= '9' && index < 1_000_000)
                {
                    index = index * 10 + text[position] - '0';
                    position++;
                }
                var endOfIndex = position;
                if (position < text.Length && text[position] is >= '0' and <= '9')
                {
                    throw new FormatException("Indexed Placeholder identity exceeds the composite-format limit.");
                }
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

                if (endOfIndex > start)
                {
                    // The framework is the syntax authority for indexed composite formatting.
                    // Remap this one identity to zero so syntax validation needs one null
                    // argument regardless of index gaps, without evaluating Argument Formats.
                    string.Format(CultureInfo.InvariantCulture, "{0" + text.Substring(endOfIndex, position - endOfIndex + 1), new object?[] { null });
                    arguments.Add(index);
                }
                else if (start < position && (char.IsLetter(text[start]) || text[start] == '_'))
                {
                    hasNamedPlaceholders = true;
                }
                else
                {
                    throw new FormatException("Expected a numeric Indexed Placeholder identity.");
                }
            }
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            throw new ResourceValidationException($"'{resourceName}' Resource Key '{key}' has a malformed Formatting Placeholder: {exception.Message}");
        }

        return hasNamedPlaceholders ? null : arguments.ToArray();
    }
}
