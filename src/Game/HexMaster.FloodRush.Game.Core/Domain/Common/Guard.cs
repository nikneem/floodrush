namespace HexMaster.FloodRush.Game.Core.Domain.Common;

internal static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }

    public static int AgainstNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be zero or greater.");
        }

        return value;
    }

    public static int AgainstOutOfRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{parameterName} must be between {minimum} and {maximum}.");
        }

        return value;
    }

    public static T AgainstNull<T>(T? value, string parameterName)
        where T : class
    {
        return value ?? throw new ArgumentNullException(parameterName);
    }
}
