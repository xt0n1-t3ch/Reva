using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Reva.App.Onboarding;

public static class TourTargetResolver
{
    public static Rect? ResolveBounds(Visual? root, Visual? coordinateSpace, string? targetName)
    {
        if (root is null || coordinateSpace is null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        var target = FindByName(root, targetName);
        if (target is null)
        {
            return null;
        }

        if (target is Control { IsVisible: false })
        {
            return null;
        }

        if (!target.IsAttachedToVisualTree())
        {
            return null;
        }

        var bounds = target.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        var origin = target.TranslatePoint(default, coordinateSpace);
        if (origin is null)
        {
            return null;
        }

        return new Rect(origin.Value, bounds.Size);
    }

    private static Visual? FindByName(Visual root, string targetName)
    {
        if (root is Control control && string.Equals(control.Name, targetName, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.GetVisualChildren())
        {
            var match = FindByName(child, targetName);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
