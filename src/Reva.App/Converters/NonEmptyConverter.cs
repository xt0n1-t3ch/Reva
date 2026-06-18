using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Reva.App.Converters;

public sealed class NonEmptyConverter : IValueConverter
{
    public static readonly NonEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
