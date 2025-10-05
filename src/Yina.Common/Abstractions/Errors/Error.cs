namespace Yina.Common.Abstractions.Errors;

public sealed record Error(string Code, string Message, int StatusCode = 400)
{
    public static readonly Error None = new(string.Empty, string.Empty, 200);

    public bool IsNone => string.IsNullOrWhiteSpace(Code);

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Message : $"{Code}: {Message}";
}
