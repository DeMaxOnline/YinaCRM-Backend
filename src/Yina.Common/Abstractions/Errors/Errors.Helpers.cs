namespace Yina.Common.Abstractions.Errors;

public static partial class Errors
{
    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        var normalized = new string(code.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray());
        while (normalized.Contains("__"))
        {
            normalized = normalized.Replace("__", "_");
        }

        return normalized.Trim('_');
    }
}
