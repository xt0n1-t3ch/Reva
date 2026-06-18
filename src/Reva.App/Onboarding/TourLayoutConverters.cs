using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Reva.App.Onboarding;

public sealed class TourCenteredHorizontalAlignmentConverter : IValueConverter
{
    public static readonly TourCenteredHorizontalAlignmentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? HorizontalAlignment.Center : HorizontalAlignment.Left;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class TourCenteredVerticalAlignmentConverter : IValueConverter
{
    public static readonly TourCenteredVerticalAlignmentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? VerticalAlignment.Center : VerticalAlignment.Top;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
