namespace Yina.Common.Foundation;

public static class Guard
{
    public static T AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        return value;
    }

    public static int AgainstNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be non-negative.");
        }

        return value;
    }

    public static void Against(bool condition, string message, string? parameterName = null)
    {
        if (!condition)
        {
            return;
        }

        if (parameterName is null)
        {
            throw new ArgumentException(message);
        }

        throw new ArgumentException(message, parameterName);
    }
}
