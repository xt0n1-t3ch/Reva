using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Reva.App.Converters;

public sealed class OnlineBrushConverter : IValueConverter
{
    public static readonly OnlineBrushConverter Instance = new();

    private static readonly IBrush Online = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
    private static readonly IBrush Offline = new SolidColorBrush(Color.FromRgb(0x8A, 0x95, 0xAD));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Online : Offline;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
