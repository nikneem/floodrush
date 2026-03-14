using System.Globalization;

namespace HexMaster.FloodRush.Game.Converters;

/// <summary>
/// Returns <c>true</c> when the bound integer value is not zero; <c>false</c> otherwise.
/// Used to hide score breakdown rows that have a zero contribution.
/// </summary>
public sealed class IsNotZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i != 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
